using System;

namespace ProjectOrigin.Chronicler.Models;

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

    public static Common.V1.FederatedStreamId ToProto(this FederatedCertificateId fid)
    {
        return new Common.V1.FederatedStreamId
        {
            Registry = fid.RegistryName,
            StreamId = new Common.V1.Uuid
            {
                Value = fid.StreamId.ToString()
            }
        };
    }
}
