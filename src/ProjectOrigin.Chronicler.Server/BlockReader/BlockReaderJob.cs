using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Models;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.Chronicler.Server.Repositories;
using ProjectOrigin.ServiceCommon.Database;

namespace ProjectOrigin.Chronicler.Server.BlockReader;

public class BlockReaderJob : IBlockReaderJob
{
    private readonly ILogger<BlockReaderJob> _logger;
    private readonly ChroniclerOptions _options;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRegistryService _registryService;

    public BlockReaderJob(
        ILogger<BlockReaderJob> logger,
        IOptions<ChroniclerOptions> options,
        IUnitOfWork unitOfWork,
        IRegistryService registryService)
    {
        _logger = logger;
        _options = options.Value;
        _unitOfWork = unitOfWork;
        _registryService = registryService;
    }

    public async Task ProcessAllRegistries(CancellationToken cancellationToken)
    {
        var registries = (await _unitOfWork.GetRepository<IChroniclerRepository>().GetRegistriesToCrawl())
            .ToList();

        foreach (var lastReadBlock in registries)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await ProcessRegistryBlocks(lastReadBlock.RegistryName, lastReadBlock.BlockHeight, cancellationToken);
        }
    }


    public async Task ProcessRegistryBlocks(string registryName, int previousBlockHeight, CancellationToken cancellationToken)
    {
        var lastProcessedBlockHeight = previousBlockHeight;

        while (!cancellationToken.IsCancellationRequested)
        {
            var block = await _registryService.GetNextBlock(registryName, lastProcessedBlockHeight);
            if (block != null)
            {
                if (block.Height != lastProcessedBlockHeight + 1)
                {
                    _logger.LogError("Block height mismatch for registry {RegistryName}. Expected {ExpectedHeight}, got {BlockHeight}.", registryName, lastProcessedBlockHeight + 1, block.Height);
                    break;
                }

                await ProcessBlock(_unitOfWork, registryName, block);
                lastProcessedBlockHeight = block.Height;
            }
            else
            {
                _logger.LogTrace("No new blocks found for registry {RegistryName} at height {BlockHeight}", registryName, lastProcessedBlockHeight + 1);
                break;
            }
        }
    }

    private async Task ProcessBlock(IUnitOfWork unitOfWork, string registryName, Registry.V1.Block block)
    {
        var repository = unitOfWork.GetRepository<IChroniclerRepository>();

        foreach (var transaction in block.Transactions)
        {
            if (transaction.Header.PayloadType == Electricity.V1.IssuedEvent.Descriptor.FullName)
            {
                await ProcessIssuedEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.AllocatedEvent.Descriptor.FullName)
            {
                await ProcessAllocatedEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.ClaimedEvent.Descriptor.FullName)
            {
                await ProcessClaimedEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.WithdrawnEvent.Descriptor.FullName)
            {
                await ProcessWithdrawnEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.UnclaimedEvent.Descriptor.FullName)
            {
                await ProcessUnclaimedEvent(repository, transaction);
            }
        }

        await repository.UpsertReadBlock(new LastReadBlock
        {
            RegistryName = registryName,
            BlockHeight = block.Height,
            ReadAt = DateTimeOffset.UtcNow,
        });

        unitOfWork.Commit();
    }

    /// <summary>
    /// Analyse an IssuedEvent and insert the certificate info into the database.
    /// </summary>
    private async Task ProcessIssuedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var issuedEvent = Electricity.V1.IssuedEvent.Parser.ParseFrom(transaction.Payload);
        if (_options.GridAreas.Contains(issuedEvent.GridArea))
        {
            var fid = transaction.Header.FederatedStreamId.ToModel();
            await repository.InsertCertificateInfo(new CertificateInfo
            {
                RegistryName = fid.RegistryName,
                CertificateId = fid.StreamId,
                StartTime = issuedEvent.Period.Start.ToDateTimeOffset(),
                EndTime = issuedEvent.Period.End.ToDateTimeOffset(),
                GridArea = issuedEvent.GridArea,
            });
        }
    }

    private async Task ProcessAllocatedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var fid = transaction.Header.FederatedStreamId.ToModel();

        var allocatedEvent = Electricity.V1.AllocatedEvent.Parser.ParseFrom(transaction.Payload);
        var allocationId = Guid.Parse(allocatedEvent.AllocationId.Value);

        var sliceCommitmentHash = allocatedEvent.ConsumptionCertificateId.Equals(transaction.Header.FederatedStreamId) ?
            allocatedEvent.ConsumptionSourceSliceHash : allocatedEvent.ProductionSourceSliceHash;

        var claimIntent = await repository.GetClaimIntent(fid, sliceCommitmentHash.ToByteArray());
        if (claimIntent != null)
        {
            await repository.InsertClaimAllocation(new ClaimAllocation
            {
                Id = Guid.NewGuid(),
                ClaimIntentId = claimIntent.Id,
                RegistryName = fid.RegistryName,
                CertificateId = fid.StreamId,
                AllocationId = allocationId,
            });
        }
        else
        {
            _logger.LogTrace("Allocation for certificate {registry}-{certificateId} not relevant", fid.RegistryName, fid.StreamId);
        }
    }

    private async Task ProcessClaimedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var fid = transaction.Header.FederatedStreamId.ToModel();
        var claimedEvent = Electricity.V1.ClaimedEvent.Parser.ParseFrom(transaction.Payload);
        var allocationId = Guid.Parse(claimedEvent.AllocationId.Value);

        var allocation = await repository.GetClaimAllocation(fid, allocationId);
        if (allocation != null)
        {
            var claimIntent = await repository.GetClaimIntent(allocation.ClaimIntentId);

            await repository.InsertClaimRecord(new ClaimRecord
            {
                Id = Guid.NewGuid(),
                RegistryName = fid.RegistryName,
                CertificateId = fid.StreamId,
                Quantity = claimIntent.Quantity,
                RandomR = claimIntent.RandomR,
                State = ClaimRecordState.Claimed
            });
            await repository.DeleteClaimIntent(claimIntent.Id);
            await repository.DeleteClaimAllocation(allocation.Id);
        }
        else
        {
            _logger.LogTrace("Claim for certificate {registry}-{certificateId} not relevant", fid.RegistryName, fid.StreamId);
        }
    }

    private async Task ProcessWithdrawnEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var fid = transaction.Header.FederatedStreamId.ToModel();

        await repository.WithdrawClaimRecord(fid);
    }

    private async Task ProcessUnclaimedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var fid = transaction.Header.FederatedStreamId.ToModel();

        await repository.UnclaimClaimRecord(fid);
    }
}
