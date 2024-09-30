using System;
using System.Data;
using System.Threading.Tasks;
using AutoFixture;
using Dapper;
using FluentAssertions;
using Npgsql;
using ProjectOrigin.Chronicler.Models;
using ProjectOrigin.Chronicler.Repositories;
using ProjectOrigin.Chronicler.Test.FixtureCustomizations;
using ProjectOrigin.TestCommon.Fixtures;
using Xunit;

namespace ProjectOrigin.Chronicler.Test;

public sealed class ChroniclerRepositoryTests : IClassFixture<PostgresDatabaseFixture<Startup>>, IDisposable
{
    private readonly PostgresDatabaseFixture<Startup> _databaseFixture;
    private readonly Fixture _fixture = new();
    private readonly IDbConnection _con;
    private readonly ChroniclerRepository _repository;

    public ChroniclerRepositoryTests(PostgresDatabaseFixture<Startup> databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _databaseFixture.ResetDatabase();
        _fixture.Customizations.Add(new MicrosecondDateTimeOffsetGenerator());

        _con = new NpgsqlConnection(_databaseFixture.HostConnectionString);
        _con.Open();
        _repository = new ChroniclerRepository(_con.BeginTransaction());
    }

    public void Dispose()
    {
        _con.Dispose();
    }

    [Fact]
    public async Task InsertClaimIntent_InsertsClaimIntent()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();

        // Act
        await _repository.InsertClaimIntent(intent);

        // Assert
        _con.QuerySingle<ClaimIntent>("SELECT * FROM claim_intents").Should().BeEquivalentTo(intent);
    }

    [Fact]
    public async Task InsertClaimIntent_InsertsClaimIntentAndLastReadBlock()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();

        // Act
        await _repository.InsertClaimIntent(intent);

        // Assert
        _con.QuerySingle<ClaimIntent>("SELECT * FROM claim_intents").Should().BeEquivalentTo(intent);
        var block = _con.QuerySingle<LastReadBlock>("SELECT * FROM read_blocks");
        block.RegistryName.Should().Be(intent.RegistryName);
        block.BlockHeight.Should().Be(-1);
    }

    [Fact]
    public async Task InsertClaimIntent_InsertsClaimIntentAndNotChangeLastReadBlock()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();
        var block = new LastReadBlock
        {
            RegistryName = intent.RegistryName,
            BlockHeight = 5,
            ReadAt = _fixture.Create<DateTimeOffset>()
        };
        await _repository.UpsertReadBlock(block);

        // Act
        await _repository.InsertClaimIntent(intent);

        // Assert
        _con.QuerySingle<ClaimIntent>("SELECT * FROM claim_intents").Should().BeEquivalentTo(intent);
        _con.QuerySingle<LastReadBlock>("SELECT * FROM read_blocks").Should().BeEquivalentTo(block);
    }

    [Fact]
    public async Task GetClaimIntent_ReturnsClaimIntent()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent);

        // Act
        var result = await _repository.GetClaimIntent(intent.Id);

        // Assert
        result.Should().BeEquivalentTo(intent);
    }

    [Fact]
    public async Task GetClaimIntent_ThrowsExceptionWhenNotFound()
    {
        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetClaimIntent(new FederatedCertificateId
        {
            RegistryName = _fixture.Create<string>(),
            StreamId = Guid.NewGuid()
        }, new byte[32]));
    }

    [Fact]
    public async Task GetClaimIntentFromFid_ReturnsClaimIntent()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent);

        // Act
        var result = await _repository.GetClaimIntent(new FederatedCertificateId
        {
            RegistryName = intent.RegistryName,
            StreamId = intent.CertificateId
        }, intent.CommitmentHash);

        // Assert
        result.Should().BeEquivalentTo(intent);
    }

    [Fact]
    public async Task GetClaimIntentFromFid_ThrowsExceptionWhenNotFound()
    {
        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetClaimIntent(new FederatedCertificateId
        {
            RegistryName = _fixture.Create<string>(),
            StreamId = Guid.NewGuid()
        }, new byte[32]));
    }

    [Fact]
    public async Task HasClaimIntent_ReturnsFalse_WhenClaimIntentDoesNotExist()
    {
        // Act
        var result = await _repository.HasClaimIntent(new FederatedCertificateId
        {
            RegistryName = _fixture.Create<string>(),
            StreamId = Guid.NewGuid()
        }, new byte[32]);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasClaimIntent_ReturnsTrue_WhenClaimIntentExists()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent);

        // Act
        var result = await _repository.HasClaimIntent(new FederatedCertificateId
        {
            RegistryName = intent.RegistryName,
            StreamId = intent.CertificateId
        }, intent.CommitmentHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasClaimIntent_ReturnsFalse_WhenOtherClaimIntentExists()
    {
        // Arrange
        var intent1 = _fixture.Create<ClaimIntent>();
        var intent2 = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent1);

        // Act
        var result = await _repository.HasClaimIntent(new FederatedCertificateId
        {
            RegistryName = intent2.RegistryName,
            StreamId = intent2.CertificateId
        }, intent2.CommitmentHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteClaimIntent_DeletesClaimIntent()
    {
        // Arrange
        var intent = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent);

        // Act
        await _repository.DeleteClaimIntent(intent.Id);

        // Assert
        Assert.Empty(_con.Query("SELECT * FROM claim_intents"));
    }

    [Fact]
    public async Task DeleteClaimIntent_DoesNotThrowExceptionWhenClaimIntentDoesNotExist()
    {
        // Arrange
        Task task = _repository.DeleteClaimIntent(Guid.NewGuid());

        // Act
        await task;

        // Assert
        task.Status.Should().Be(TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task DeleteClaimIntent_DoesNotDeleteOtherClaimIntents()
    {
        // Arrange
        var intent1 = _fixture.Create<ClaimIntent>();
        var intent2 = _fixture.Create<ClaimIntent>();
        await _repository.InsertClaimIntent(intent1);
        await _repository.InsertClaimIntent(intent2);

        // Act
        await _repository.DeleteClaimIntent(intent1.Id);

        // Assert
        Assert.Single(_con.Query("SELECT * FROM claim_intents"));
    }

    [Fact]
    public async Task UpsertReadBlock_InsertsBlock()
    {
        // Arrange
        var registry = _fixture.Create<LastReadBlock>();

        // Act
        await _repository.UpsertReadBlock(registry);

        // Assert
        _con.QuerySingle<LastReadBlock>("SELECT * FROM read_blocks").Should().BeEquivalentTo(registry);
    }

    [Fact]
    public async Task UpsertReadBlock_UpdatesBlock()
    {
        // Arrange
        var registry = _fixture.Create<LastReadBlock>();
        await _repository.UpsertReadBlock(registry);

        var updatedRegistry = new LastReadBlock
        {
            RegistryName = registry.RegistryName,
            BlockHeight = registry.BlockHeight + 1,
            ReadAt = registry.ReadAt
        };

        // Act
        await _repository.UpsertReadBlock(updatedRegistry);

        // Assert
        _con.QuerySingle<LastReadBlock>("SELECT * FROM read_blocks").Should().BeEquivalentTo(updatedRegistry);
    }

    [Fact]
    public async Task GetRegistriesToCrawl_ReturnsRegistries()
    {
        // Arrange
        var registry1 = _fixture.Create<LastReadBlock>();
        var registry2 = _fixture.Create<LastReadBlock>();
        await _repository.UpsertReadBlock(registry1);
        await _repository.UpsertReadBlock(registry2);

        // Act
        var result = await _repository.GetRegistriesToCrawl();

        // Assert
        result.Should().BeEquivalentTo(new[] { registry1, registry2 });
    }

    [Fact]
    public async Task GetRegistriesToCrawl_ReturnsEmptyListWhenNoRegistries()
    {
        // Act
        var result = await _repository.GetRegistriesToCrawl();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task InsertCertificateInfo_InsertsCertificateInfo()
    {
        // Arrange
        var certificateInfo = _fixture.Create<CertificateInfo>();

        // Act
        await _repository.InsertCertificateInfo(certificateInfo);

        // Assert
        _con.QuerySingle<CertificateInfo>("SELECT * FROM certificate_infos").Should().BeEquivalentTo(certificateInfo);
    }

    [Fact]
    public async Task InsertClaimAllocation_InsertsClaimAllocation()
    {
        // Arrange
        var allocation = _fixture.Create<ClaimAllocation>();

        // Act
        await _repository.InsertClaimAllocation(allocation);

        // Assert
        _con.QuerySingle<ClaimAllocation>("SELECT * FROM claim_allocations").Should().BeEquivalentTo(allocation);
    }

    [Fact]
    public async Task GetClaimAllocation_ReturnsClaimAllocation()
    {
        // Arrange
        var allocation = _fixture.Create<ClaimAllocation>();
        await _repository.InsertClaimAllocation(allocation);

        // Act
        var result = await _repository.GetClaimAllocation(new FederatedCertificateId
        {
            RegistryName = allocation.RegistryName,
            StreamId = allocation.CertificateId
        }, allocation.AllocationId);

        // Assert
        result.Should().BeEquivalentTo(allocation);
    }

    [Fact]
    public async Task GetClaimAllocation_ThrowsExceptionWhenNotFound()
    {
        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.GetClaimAllocation(new FederatedCertificateId
        {
            RegistryName = _fixture.Create<string>(),
            StreamId = Guid.NewGuid()
        }, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteClaimAllocation_DeletesClaimAllocation()
    {
        // Arrange
        var allocation = _fixture.Create<ClaimAllocation>();
        await _repository.InsertClaimAllocation(allocation);

        // Act
        await _repository.DeleteClaimAllocation(allocation.Id);

        // Assert
        Assert.Empty(_con.Query("SELECT * FROM claim_allocations"));
    }

    [Fact]
    public async Task InsertClaimRecord_InsertsClaimRecord()
    {
        // Arrange
        var record = _fixture.Create<ClaimRecord>();

        // Act
        await _repository.InsertClaimRecord(record);

        // Assert
        _con.QuerySingle<ClaimRecord>("SELECT * FROM claim_records").Should().BeEquivalentTo(record);
    }

    [Fact]
    public async Task WithdrawClaimRecord_WithdrawsClaimRecord()
    {
        var record1 = new ClaimRecord()
        {
            RegistryName = _fixture.Create<string>(),
            State = ClaimRecordState.Claimed,
            CertificateId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Quantity = 123,
            RandomR = _fixture.Create<byte[]>()
        };
        var record2 = new ClaimRecord()
        {
            RegistryName = _fixture.Create<string>(),
            State = ClaimRecordState.Claimed,
            CertificateId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Quantity = 124,
            RandomR = _fixture.Create<byte[]>()
        };
        await _repository.InsertClaimRecord(record1);
        await _repository.InsertClaimRecord(record2);

        await _repository.WithdrawClaimRecord(new FederatedCertificateId
        {
            RegistryName = record1.RegistryName,
            StreamId = record1.CertificateId
        });

        var withdrawnRecord = await _con.QuerySingleAsync<ClaimRecord>(@"SELECT * FROM claim_records
                                                            WHERE registry_name = @registryName
                                                            AND certificate_id = @certificateId",
            new
            {
                registryName = record1.RegistryName,
                certificateId = record1.CertificateId
            });

        withdrawnRecord.State.Should().Be(ClaimRecordState.Withdrawn);
    }
}
