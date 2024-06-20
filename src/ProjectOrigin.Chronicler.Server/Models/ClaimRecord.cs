using System;

namespace ProjectOrigin.Chronicler.Server.Models;

public record ClaimRecord
{
    public required Guid Id { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required long Quantity { get; init; }
    public required byte[] RandomR { get; init; }
}
