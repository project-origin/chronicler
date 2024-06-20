using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Models;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.ServiceCommon.Database;

namespace ProjectOrigin.Chronicler.Server.Repositories;

public class BlockReaderJob
{
    private readonly string[] zones = ["DK1"];
    private readonly ILogger<BlockReaderJob> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<RegistryOptions> _options;

    public BlockReaderJob(ILogger<BlockReaderJob> logger, IUnitOfWork unitOfWork, IOptions<RegistryOptions> options)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        var registries = (await _unitOfWork.GetRepository<IChroniclerRepository>().GetRegistriesToCrawl()).ToList();
        foreach (var registry in registries)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var lastBlock = registry;
            do
            {
                lastBlock = await ReadNextBlock(_unitOfWork, lastBlock);
            }
            while (lastBlock != null);
        }
    }

    private async Task<ReadBlock?> ReadNextBlock(IUnitOfWork unitOfWork, ReadBlock lastReadBlock)
    {
        if (!_options.Value.RegistryUrls.TryGetValue(lastReadBlock.RegistryName, out var registryUrl))
        {
            _logger.LogError("Registry with name {RegistryName} not found in configuration.", lastReadBlock.RegistryName);
            return null;
        }

        using var channel = GrpcChannel.ForAddress(registryUrl);
        var client = new Registry.V1.RegistryService.RegistryServiceClient(channel);
        var response = await client.GetBlocksAsync(new Registry.V1.GetBlocksRequest
        {
            Skip = lastReadBlock.BlockHeight,
            Limit = 1,
            IncludeTransactions = true,
        });
        var block = response.Blocks.SingleOrDefault();

        if (block == null)
        {
            _logger.LogWarning("No new blocks found for registry {RegistryName} at height {BlockHeight}", lastReadBlock.RegistryName, lastReadBlock.BlockHeight);
            return null;
        }

        var repository = unitOfWork.GetRepository<IChroniclerRepository>();

        foreach (var transaction in block.Transactions)
        {
            if (transaction.Header.PayloadType == Electricity.V1.IssuedEvent.Descriptor.FullName)
            {
                await AnalyseIssuedEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.AllocatedEvent.Descriptor.FullName)
            {
                await AnalyseAllocatedEvent(repository, transaction);
            }
            else if (transaction.Header.PayloadType == Electricity.V1.ClaimedEvent.Descriptor.FullName)
            {
                await AnalyseClaimedEvent(repository, transaction);
            }
        }

        var newReadBlock = new ReadBlock
        {
            RegistryName = lastReadBlock.RegistryName,
            BlockHeight = block.Height,
            ReadAt = DateTimeOffset.UtcNow,
        };
        await repository.UpsertReadBlock(newReadBlock);

        unitOfWork.Commit();

        return newReadBlock;
    }

    /// <summary>
    /// Analyse an IssuedEvent and insert the certificate info into the database.
    /// </summary>
    private async Task AnalyseIssuedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var issuedEvent = Electricity.V1.IssuedEvent.Parser.ParseFrom(transaction.Payload);
        if (zones.Contains(issuedEvent.GridArea))
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

    private async Task AnalyseAllocatedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
    {
        var fid = transaction.Header.FederatedStreamId.ToModel();

        var allocatedEvent = Electricity.V1.AllocatedEvent.Parser.ParseFrom(transaction.Payload);
        var allocationId = Guid.Parse(allocatedEvent.AllocationId.Value);

        var sliceCommitmentHash = allocatedEvent.ConsumptionCertificateId == transaction.Header.FederatedStreamId ?
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

    private async Task AnalyseClaimedEvent(IChroniclerRepository repository, Registry.V1.Transaction transaction)
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
            });
            await repository.DeleteClaimIntent(claimIntent.Id);
            await repository.DeleteClaimAllocation(allocation.Id);
        }
        else
        {
            _logger.LogTrace("Claim for certificate {registry}-{certificateId} not relevant", fid.RegistryName, fid.StreamId);
        }
    }

}
