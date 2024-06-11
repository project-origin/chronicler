using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.Chronicler.Server;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.TestCommon;
using ProjectOrigin.TestCommon.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace ProjectOrigin.Chronicler.Test;

public class ChroniclerServiceTests : TestServerBase<Startup>, IClassFixture<TestServerFixture<Startup>>, IClassFixture<PostgresDatabaseFixture<Startup>>
{
    private readonly Fixture _fixture = new();
    private readonly IPrivateKey _privateKey = Algorithms.Ed25519.GenerateNewPrivateKey();
    private readonly PostgresDatabaseFixture<Startup> _databaseFixture;

    public ChroniclerServiceTests(
        TestServerFixture<Startup> serverFixture,
        PostgresDatabaseFixture<Startup> databaseFixture,
        ITestOutputHelper outputHelper) : base(serverFixture, outputHelper)
    {
        _databaseFixture = databaseFixture;
        serverFixture.ConfigureHostConfiguration(new()
        {
            { "ConnectionStrings:Database", databaseFixture.HostConnectionString },
            { "Chronicler:SigningKeyFilename", TempFile.WriteAllText(_privateKey.ExportPkixText()) },
        });
    }

    [Fact]
    public async Task ServiceReturnsSingature()
    {
        // Arrange
        var registryName = _fixture.Create<string>();
        var certId = _fixture.Create<Guid>();
        var client = new V1.RegistryService.RegistryServiceClient(Channel);
        var quantity = _fixture.Create<int>();
        var commitmentInfo = new SecretCommitmentInfo((uint)quantity);

        var request = new V1.ClaimIntentRequest()
        {
            CertificateId = new Common.V1.FederatedStreamId()
            {
                Registry = registryName,
                StreamId = new Common.V1.Uuid() { Value = certId.ToString(), }
            },
            Quantity = quantity,
            RandomR = ByteString.CopyFrom(commitmentInfo.BlindingValue)
        };

        // Act
        var result = await client.RegisterClaimIntentAsync(request);

        // Assert
        result.Signature.Should().NotBeNull();
        result.Signature.Should().NotBeEmpty();
        result.Signature.Should().BeEquivalentTo(CalculateSignature(request.CertificateId, commitmentInfo, _privateKey));
    }

    private byte[] CalculateSignature(Common.V1.FederatedStreamId federatedStreamId, SecretCommitmentInfo commitmentInfo, IPrivateKey privateKey)
    {
        V1.ClaimIntent claimIntent = new()
        {
            CertificateId = federatedStreamId,
            Commitment = ByteString.CopyFrom(commitmentInfo.Commitment.C),
        };

        return privateKey.Sign(claimIntent.ToByteArray()).ToArray();
    }
}
