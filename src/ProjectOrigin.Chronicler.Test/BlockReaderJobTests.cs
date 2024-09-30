using System.Threading.Tasks;
using Xunit;
using ProjectOrigin.Chronicler.Server.BlockReader;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Options;
using System;
using ProjectOrigin.ServiceCommon.Database;
using ProjectOrigin.Chronicler.Server;
using ProjectOrigin.Chronicler.Server.Repositories;
using Google.Protobuf;
using ProjectOrigin.Chronicler.Server.Models;
using AutoFixture;
using Google.Protobuf.WellKnownTypes;

namespace ProjectOrigin.Chronicler.Test;

public class BlockReaderJobTests
{
    private const string RegistryName = "someRegistry";
    private const string GridArea = "Narnia";
    private readonly Mock<IChroniclerRepository> _repository;
    private readonly Mock<ILogger<BlockReaderJob>> _logger;
    private Mock<IRegistryService> _registryService;
    private BlockReaderJob _job;

    public BlockReaderJobTests()
    {
        _repository = new Mock<IChroniclerRepository>();
        _logger = new Mock<ILogger<BlockReaderJob>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.GetRepository<IChroniclerRepository>()).Returns(_repository.Object);
        _registryService = new Mock<IRegistryService>();
        _job = new BlockReaderJob(
            _logger.Object,
            Options.Create(new ChroniclerOptions
            {
                SigningKeyFilename = "",
                GridAreas = [GridArea],
                JobInterval = TimeSpan.FromSeconds(1),
            }),
            unitOfWork.Object,
            _registryService.Object);

    }

    [Fact]
    public async Task Verify_GetNextBlock_Called()
    {
        // Arrange

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 0), Times.Once);
        _registryService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_GetNextBlock_IfReturnedCallsNext()
    {
        // Arrange
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 1,
        });

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 0), Times.Once);
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 1), Times.Once);
        _registryService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_GetNextBlock_IfReturnedCallsNextAndNext()
    {
        // Arrange
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 1,
        });
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 1)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 2,
        });


        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 0), Times.Once);
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 1), Times.Once);
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 2), Times.Once);
        _registryService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_GetNextBlock_LogErrorIfOutOfOrder()
    {
        // Arrange
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 1,
        });
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 1)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 3,
        });

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 0), Times.Once);
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 1), Times.Once);
        _registryService.Verify(x => x.GetNextBlock(RegistryName, 2), Times.Never);
        _registryService.VerifyNoOtherCalls();

        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.VerifyNoOtherCalls();

        _logger.Verify(x => x.Log(LogLevel.Error, 0, It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Verify_CertificateInfo_Inserted()
    {
        // Arrange
        var fixture = new Fixture();
        var streamId = fixture.Create<Guid>();
        var start = fixture.Create<DateTimeOffset>();
        var end = fixture.Create<DateTimeOffset>();

        var block = new Registry.V1.Block
        {
            Height = 1,
        };
        block.AddIssued(new FederatedCertificateId { RegistryName = RegistryName, StreamId = streamId }, start, end, GridArea);
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.InsertCertificateInfo(It.Is<CertificateInfo>(
            x => x.GridArea == GridArea
            && x.RegistryName == RegistryName
            && x.CertificateId == streamId
            && x.StartTime == start
            && x.EndTime == end
            )), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Verify_Allocated_NotFound_NotInserted(bool isProduction)
    {
        // Arrange
        var fixture = new Fixture();
        var certificateId = new FederatedCertificateId { RegistryName = RegistryName, StreamId = fixture.Create<Guid>() };
        var allocationId = fixture.Create<Guid>();
        var sliceHash = fixture.Create<byte[]>();
        var block = new Registry.V1.Block
        {
            Height = 1,
        };
        if (isProduction)
        {
            block.AddProductionAllocated(allocationId, certificateId, sliceHash);
        }
        else
        {
            block.AddConsumptionAllocated(allocationId, certificateId, sliceHash);
        }
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.GetClaimIntent(certificateId, It.IsAny<byte[]>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Verify_ConsumptionAllocated_Found_Inserted(bool isProduction)
    {
        // Arrange
        var fixture = new Fixture();
        var claimIntentId = Guid.NewGuid();
        var certificateId = new FederatedCertificateId { RegistryName = RegistryName, StreamId = fixture.Create<Guid>() };
        var allocationId = fixture.Create<Guid>();
        var sliceHash = fixture.Create<byte[]>();
        var block = new Registry.V1.Block
        {
            Height = 1,
        };

        if (isProduction)
        {
            block.AddProductionAllocated(allocationId, certificateId, sliceHash);
        }
        else
        {
            block.AddConsumptionAllocated(allocationId, certificateId, sliceHash);
        }

        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);
        _repository.Setup(x => x.GetClaimIntent(certificateId, sliceHash)).ReturnsAsync(new ClaimIntent
        {
            Id = claimIntentId,
            RegistryName = certificateId.RegistryName,
            CertificateId = certificateId.StreamId,
            CommitmentHash = sliceHash,
            Quantity = fixture.Create<int>(),
            RandomR = fixture.Create<byte[]>(),
        });

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.GetClaimIntent(certificateId, sliceHash), Times.Once);
        _repository.Verify(x => x.InsertClaimAllocation(It.Is<ClaimAllocation>(
            x => x.Id != Guid.Empty
            && x.AllocationId == allocationId
            && x.RegistryName == certificateId.RegistryName
            && x.CertificateId == certificateId.StreamId
            && x.ClaimIntentId == claimIntentId
            )), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_Claimed_NotFound_NotInserted()
    {
        // Arrange
        var fixture = new Fixture();
        var certificateId = new FederatedCertificateId { RegistryName = RegistryName, StreamId = fixture.Create<Guid>() };
        var allocationId = fixture.Create<Guid>();
        var sliceHash = fixture.Create<byte[]>();
        var block = new Registry.V1.Block
        {
            Height = 1,
        };
        block.AddClaim(allocationId, certificateId);
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.GetClaimAllocation(certificateId, allocationId), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessWithdrawnEvent_WithdrawsClaim()
    {
        // Arrange
        var fixture = new Fixture();
        var certificateId = new FederatedCertificateId { RegistryName = RegistryName, StreamId = fixture.Create<Guid>() };
        var block = new Registry.V1.Block
        {
            Height = 1,
        };
        block.AddWithdrawn(certificateId);
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.WithdrawClaimRecord(certificateId), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_Claimed_Found_Inserted()
    {
        // Arrange
        var fixture = new Fixture();
        var claimIntentId = fixture.Create<Guid>();

        var certificateId = new FederatedCertificateId { RegistryName = RegistryName, StreamId = fixture.Create<Guid>() };
        var allocationId = fixture.Create<Guid>();
        var sliceHash = fixture.Create<byte[]>();
        var quantity = fixture.Create<int>();
        var randomR = fixture.Create<byte[]>();

        var block = new Registry.V1.Block
        {
            Height = 1,
        };
        block.AddClaim(allocationId, certificateId);
        _registryService.Setup(x => x.GetNextBlock(RegistryName, 0)).ReturnsAsync(block);
        _repository.Setup(x => x.GetClaimIntent(claimIntentId)).ReturnsAsync(new ClaimIntent
        {
            Id = claimIntentId,
            RegistryName = certificateId.RegistryName,
            CertificateId = certificateId.StreamId,
            CommitmentHash = sliceHash,
            Quantity = quantity,
            RandomR = randomR,
        });
        _repository.Setup(x => x.GetClaimAllocation(certificateId, allocationId)).ReturnsAsync(new ClaimAllocation
        {
            Id = Guid.NewGuid(),
            ClaimIntentId = claimIntentId,
            AllocationId = allocationId,
            CertificateId = certificateId.StreamId,
            RegistryName = certificateId.RegistryName,
        });

        // Act
        await _job.ProcessRegistryBlocks(RegistryName, 0, default);

        // Assert
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.BlockHeight == 1)), Times.Once);
        _repository.Verify(x => x.GetClaimAllocation(certificateId, allocationId), Times.Once);
        _repository.Verify(x => x.GetClaimIntent(claimIntentId), Times.Once);
        _repository.Verify(x => x.InsertClaimRecord(It.Is<ClaimRecord>(
            x => x.Id != Guid.Empty
            && x.RegistryName == certificateId.RegistryName
            && x.CertificateId == certificateId.StreamId
            && x.Quantity == quantity
            && x.RandomR == randomR
            )), Times.Once);
        _repository.Verify(x => x.DeleteClaimIntent(claimIntentId), Times.Once);
        _repository.Verify(x => x.DeleteClaimAllocation(It.IsAny<Guid>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Verify_CrawlAllRegistries()
    {
        // Arrange
        _repository.Setup(x => x.GetRegistriesToCrawl()).ReturnsAsync(
        [
            new LastReadBlock { RegistryName = "registry1", BlockHeight = 0, ReadAt = DateTimeOffset.UtcNow - TimeSpan.FromDays(1) },
            new LastReadBlock { RegistryName = "registry2", BlockHeight = 3, ReadAt = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1) },
        ]);

        _registryService.Setup(x => x.GetNextBlock("registry2", 3)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 4,
        });
        _registryService.Setup(x => x.GetNextBlock("registry2", 4)).ReturnsAsync(new Registry.V1.Block
        {
            Height = 5,
        });

        // Act
        await _job.ProcessAllRegistries(default);

        // Assert
        _logger.Verify(x => x.Log(LogLevel.Trace, 0, It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        _logger.VerifyNoOtherCalls();

        _registryService.Verify(x => x.GetNextBlock("registry1", 0), Times.Once);
        _registryService.Verify(x => x.GetNextBlock("registry2", 3), Times.Once);
        _registryService.Verify(x => x.GetNextBlock("registry2", 4), Times.Once);
        _registryService.Verify(x => x.GetNextBlock("registry2", 5), Times.Once);
        _registryService.VerifyNoOtherCalls();

        _repository.Verify(x => x.GetRegistriesToCrawl(), Times.Once);
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.RegistryName == "registry2" && x.BlockHeight == 4)), Times.Once);
        _repository.Verify(x => x.UpsertReadBlock(It.Is<LastReadBlock>(x => x.RegistryName == "registry2" && x.BlockHeight == 5)), Times.Once);
        _repository.VerifyNoOtherCalls();

    }
}

public static class BlockExtensions
{
    public static void AddIssued(this Registry.V1.Block block, FederatedCertificateId id, DateTimeOffset start, DateTimeOffset end, string area)
    {
        block.Transactions.Add(new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                PayloadType = Electricity.V1.IssuedEvent.Descriptor.FullName,
                FederatedStreamId = id.ToProto(),
            },
            Payload = new Electricity.V1.IssuedEvent
            {
                GridArea = area,
                Period = new Electricity.V1.DateInterval
                {
                    Start = Timestamp.FromDateTimeOffset(start),
                    End = Timestamp.FromDateTimeOffset(end),
                },
            }.ToByteString()
        });
    }

    public static void AddConsumptionAllocated(this Registry.V1.Block block, Guid allocationId, FederatedCertificateId id, byte[] sliceHash)
    {
        block.Transactions.Add(new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                PayloadType = Electricity.V1.AllocatedEvent.Descriptor.FullName,
                FederatedStreamId = id.ToProto(),
            },
            Payload = new Electricity.V1.AllocatedEvent
            {
                AllocationId = new Common.V1.Uuid { Value = allocationId.ToString() },
                ConsumptionCertificateId = id.ToProto(),
                ProductionCertificateId = new Common.V1.FederatedStreamId
                {
                    Registry = id.RegistryName,
                    StreamId = new Common.V1.Uuid { Value = Guid.NewGuid().ToString() },
                },
                ConsumptionSourceSliceHash = ByteString.CopyFrom(sliceHash),
                ProductionSourceSliceHash = ByteString.Empty,
                EqualityProof = ByteString.Empty,
            }.ToByteString()
        });
    }

    public static void AddProductionAllocated(this Registry.V1.Block block, Guid allocationId, FederatedCertificateId id, byte[] sliceHash)
    {
        block.Transactions.Add(new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                PayloadType = Electricity.V1.AllocatedEvent.Descriptor.FullName,
                FederatedStreamId = id.ToProto(),
            },
            Payload = new Electricity.V1.AllocatedEvent
            {
                AllocationId = new Common.V1.Uuid { Value = allocationId.ToString() },
                ConsumptionCertificateId = new Common.V1.FederatedStreamId
                {
                    Registry = id.RegistryName,
                    StreamId = new Common.V1.Uuid { Value = Guid.NewGuid().ToString() },
                },
                ProductionCertificateId = id.ToProto(),
                ConsumptionSourceSliceHash = ByteString.Empty,
                ProductionSourceSliceHash = ByteString.CopyFrom(sliceHash),
                EqualityProof = ByteString.Empty,
            }.ToByteString()
        });
    }

    public static void AddClaim(this Registry.V1.Block block, Guid allocationId, FederatedCertificateId id)
    {
        block.Transactions.Add(new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                PayloadType = Electricity.V1.ClaimedEvent.Descriptor.FullName,
                FederatedStreamId = id.ToProto(),
            },
            Payload = new Electricity.V1.ClaimedEvent
            {
                AllocationId = new Common.V1.Uuid { Value = allocationId.ToString() },
                CertificateId = id.ToProto(),
            }.ToByteString()
        });
    }

    public static void AddWithdrawn(this Registry.V1.Block block, FederatedCertificateId id)
    {
        block.Transactions.Add(new Registry.V1.Transaction
        {
            Header = new Registry.V1.TransactionHeader
            {
                PayloadType = Electricity.V1.WithdrawnEvent.Descriptor.FullName,
                FederatedStreamId = id.ToProto(),
            },
            Payload = new Electricity.V1.WithdrawnEvent().ToByteString()
        });
    }
}

