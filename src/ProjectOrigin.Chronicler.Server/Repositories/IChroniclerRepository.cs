using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.Chronicler.Server.Models;

namespace ProjectOrigin.Chronicler.Server.Repositories;

public interface IChroniclerRepository
{
    Task InsertClaimIntent(ClaimIntent certificate);
    Task<bool> HasClaimIntent(FederatedCertificateId fid, byte[] sliceCommitmentHash);
    Task<ClaimIntent> GetClaimIntent(Guid claimIntentId);
    Task<ClaimIntent> GetClaimIntent(FederatedCertificateId fid, byte[] sliceCommitmentHash);
    Task DeleteClaimIntent(Guid id);

    Task<IEnumerable<LastReadBlock>> GetRegistriesToCrawl();
    Task UpsertReadBlock(LastReadBlock block);

    Task InsertCertificateInfo(CertificateInfo certificateInfo);

    Task InsertClaimAllocation(ClaimAllocation allocation);
    Task<ClaimAllocation> GetClaimAllocation(FederatedCertificateId fid, Guid allocationId);
    Task DeleteClaimAllocation(Guid id);

    Task InsertClaimRecord(ClaimRecord record);

}
