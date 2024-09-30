using System;

namespace ProjectOrigin.Chronicler.Models;

public record ClaimIntent
{
    public required Guid Id { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required long Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required byte[] CommitmentHash { get; init; }
}
