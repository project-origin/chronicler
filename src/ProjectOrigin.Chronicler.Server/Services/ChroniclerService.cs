using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Models;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.ServiceCommon.DataPersistence;
using ProjectOrigin.WalletSystem.Server.Repositories;

namespace ProjectOrigin.Chronicler.Server.Services;

public class ChroniclerService : V1.RegistryService.RegistryServiceBase
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
            Commitment = commitmentInfo.Commitment.C.ToArray(),
        };

        await _unitOfWork.GetRepository<IChroniclerRepository>().InsertClaimIntent(claimIntent);
        _unitOfWork.Commit();

        var obj = new V1.ClaimIntent()
        {
            CertificateId = request.CertificateId,
            Commitment = ByteString.CopyFrom(commitmentInfo.Commitment.C),
        };

        var signature = _signingKey.Sign(obj.ToByteArray()).ToArray();

        return new V1.ClaimIntentResponse()
        {
            Signature = ByteString.CopyFrom(signature)
        };
    }
}