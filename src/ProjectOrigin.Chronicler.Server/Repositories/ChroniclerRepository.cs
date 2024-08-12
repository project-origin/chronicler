using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Chronicler.Server.Models;
using ProjectOrigin.ServiceCommon.Database;

namespace ProjectOrigin.Chronicler.Server.Repositories;

public class ChroniclerRepository : AbstractRepository, IChroniclerRepository
{
    public ChroniclerRepository(IDbTransaction transaction) : base(transaction)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public Task InsertClaimIntent(ClaimIntent certificate)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO claim_intents(id, registry_name, certificate_id, quantity, random_r, commitment_hash)
              VALUES (@id, @registryName, @certificateId, @quantity, @randomR, @commitmentHash)",
            certificate);
    }

    public Task<bool> HasClaimIntent(FederatedCertificateId fid, byte[] sliceCommitmentHash)
    {
        return Connection.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1
                FROM claim_intents
                WHERE registry_name = @registryName
                  AND certificate_id = @streamId
                  AND commitment_hash = @sliceCommitmentHash)",
            new
            {
                fid.RegistryName,
                fid.StreamId,
                sliceCommitmentHash
            });
    }

    public Task<ClaimIntent> GetClaimIntent(Guid claimIntentId)
    {
        return Connection.QuerySingleAsync<ClaimIntent>(
            @"SELECT id, registry_name, certificate_id, quantity, random_r, commitment_hash
              FROM claim_intents
              WHERE id = @claimIntentId",
            new
            {
                claimIntentId
            });
    }

    public Task<ClaimIntent> GetClaimIntent(FederatedCertificateId fid, byte[] sliceCommitmentHash)
    {
        return Connection.QuerySingleAsync<ClaimIntent>(
            @"SELECT id, registry_name, certificate_id, quantity, random_r, commitment_hash
              FROM claim_intents
              WHERE registry_name = @registryName
                AND certificate_id = @streamId
                AND commitment_hash = @sliceCommitmentHash",
            new
            {
                fid.RegistryName,
                fid.StreamId,
                sliceCommitmentHash
            });
    }

    public Task DeleteClaimIntent(Guid id)
    {
        return Connection.ExecuteAsync(
            @"DELETE FROM claim_intents
              WHERE id = @id",
            new
            {
                id
            });
    }

    public Task<IEnumerable<LastReadBlock>> GetRegistriesToCrawl()
    {
        return Connection.QueryAsync<LastReadBlock>(
            @"SELECT registry_name, block_height, read_at
              FROM read_blocks");
    }

    public Task UpsertReadBlock(LastReadBlock block)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO read_blocks(registry_name, block_height, read_at)
              VALUES (@registryName, @blockHeight, @readAt)
              ON CONFLICT (registry_name) DO UPDATE SET block_height = GREATEST(read_blocks.block_height, @blockHeight), read_at = @readAt",
            block);
    }

    public Task InsertCertificateInfo(CertificateInfo certificateInfo)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO certificate_infos(registry_name, certificate_id, start_time, end_time, grid_area)
              VALUES (@registryName, @certificateId, @startTime, @endTime, @gridArea)",
            certificateInfo);
    }

    public Task InsertClaimAllocation(ClaimAllocation allocation)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO claim_allocations(id, claim_intent_id, registry_name, certificate_id, allocation_id)
              VALUES (@id, @claimIntentId, @registryName, @certificateId, @allocationId)",
            allocation);
    }

    public Task<ClaimAllocation> GetClaimAllocation(FederatedCertificateId fid, Guid allocationId)
    {
        return Connection.QuerySingleAsync<ClaimAllocation>(
            @"SELECT id, claim_intent_id, registry_name, certificate_id, allocation_id
              FROM claim_allocations
              WHERE registry_name = @registryName
                AND certificate_id = @certificateId
                AND allocation_id = @allocationId",
            new
            {
                registryName = fid.RegistryName,
                certificateId = fid.StreamId,
                allocationId
            });
    }

    public Task DeleteClaimAllocation(Guid id)
    {
        return Connection.ExecuteAsync(
            @"DELETE FROM claim_allocations
              WHERE id = @id",
            new
            {
                id
            });
    }

    public Task InsertClaimRecord(ClaimRecord record)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO claim_records(id, registry_name, certificate_id, quantity, random_r)
              VALUES (@id, @registryName, @certificateId, @quantity, @randomR)",
            record);
    }
}
