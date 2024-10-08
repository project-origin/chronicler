using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Models;
using ProjectOrigin.Chronicler.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.ServiceCommon.Database;
using ProjectOrigin.Chronicler.Repositories;
using System.Security.Cryptography;

namespace ProjectOrigin.Chronicler.Services;

public class ChroniclerService : V1.ChroniclerService.ChroniclerServiceBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPrivateKey _signingKey;

    public ChroniclerService(IUnitOfWork unitOfWork, IOptions<ChroniclerOptions> chroniclerOptions)
    {
        _unitOfWork = unitOfWork;
        _signingKey = chroniclerOptions.Value.GetPrivateKey();
    }

    public override async Task<V1.ClaimIntentResponse> RegisterClaimIntent(V1.ClaimIntentRequest request, ServerCallContext context)
    {
        var randomR = request.RandomR.ToByteArray();
        var commitmentInfo = new SecretCommitmentInfo((uint)request.Quantity, randomR);
        var claimIntent = new ClaimIntent
        {
            Id = Guid.NewGuid(),
            RegistryName = request.CertificateId.Registry,
            CertificateId = Guid.Parse(request.CertificateId.StreamId.Value),
            Quantity = request.Quantity,
            RandomR = randomR,
            CommitmentHash = SHA256.HashData(commitmentInfo.Commitment.C),
        };

        await _unitOfWork.GetRepository<IChroniclerRepository>().InsertClaimIntent(claimIntent);
        _unitOfWork.Commit();

        return new V1.ClaimIntentResponse()
        {
            Signature = ByteString.CopyFrom(SignIntent(request.CertificateId, commitmentInfo))
        };
    }

    private ReadOnlySpan<byte> SignIntent(Common.V1.FederatedStreamId certificateId, SecretCommitmentInfo commitmentInfo)
    {
        var bytes = new V1.ClaimIntent()
        {
            CertificateId = certificateId,
            Commitment = ByteString.CopyFrom(commitmentInfo.Commitment.C),
        }.ToByteArray();

        return _signingKey.Sign(bytes);
    }
}
