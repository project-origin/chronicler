using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.Chronicler.Options;
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

    public ChroniclerServiceTests(
        TestServerFixture<Startup> serverFixture,
        PostgresDatabaseFixture<Startup> databaseFixture,
        ITestOutputHelper outputHelper) : base(serverFixture, outputHelper)
    {
        var path = TempFile.WriteAllText(JsonSerializer.Serialize(new
        {
            Registries = new Dictionary<string, RegistryInfo>
            {
                { "ExampleRegistry", new RegistryInfo { Url = "http://example.com" }}
            }
        }), ".json");

        serverFixture.ConfigureHostConfiguration(new()
        {
            { "ConnectionStrings:Database", databaseFixture.HostConnectionString},
            { "Chronicler:SigningKeyFilename", TempFile.WriteAllText(_privateKey.ExportPkixText()) },
            { "Chronicler:JobInterval", "00:15:00" },
            { "Network:ConfigurationUri",  "file://"+path },
            { "Network:RefreshInterval", "00:15:00" },
        });
    }

    [Fact]
    public async Task ServiceReturnsSingature()
    {
        // Arrange
        var registryName = _fixture.Create<string>();
        var certId = _fixture.Create<Guid>();
        var client = new V1.ChroniclerService.ChroniclerServiceClient(Channel);
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

    private static byte[] CalculateSignature(Common.V1.FederatedStreamId federatedStreamId, SecretCommitmentInfo commitmentInfo, IPrivateKey privateKey)
    {
        V1.ClaimIntent claimIntent = new()
        {
            CertificateId = federatedStreamId,
            Commitment = ByteString.CopyFrom(commitmentInfo.Commitment.C),
        };

        return privateKey.Sign(claimIntent.ToByteArray()).ToArray();
    }
}
