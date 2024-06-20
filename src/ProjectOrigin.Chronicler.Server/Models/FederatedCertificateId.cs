using System;

namespace ProjectOrigin.Chronicler.Server.Models;

public record FederatedCertificateId
{
    public required string RegistryName { get; init; }
    public required Guid StreamId { get; init; }
}

public static class FederatedCertificateIdExtensions
{
    public static FederatedCertificateId ToModel(this Common.V1.FederatedStreamId fid)
    {
        return new FederatedCertificateId
        {
            RegistryName = fid.Registry,
            StreamId = Guid.Parse(fid.StreamId.Value)
        };
    }
}
