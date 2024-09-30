using System;

namespace ProjectOrigin.Chronicler.Models;

public record CertificateInfo
{
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required string GridArea { get; init; }
}
