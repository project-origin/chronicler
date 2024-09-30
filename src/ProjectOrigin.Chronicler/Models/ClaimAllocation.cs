using System;

namespace ProjectOrigin.Chronicler.Models;

public record ClaimAllocation
{
    public required Guid Id { get; init; }
    public required Guid ClaimIntentId { get; init; }
    public required string RegistryName { get; init; }
    public required Guid CertificateId { get; init; }
    public required Guid AllocationId { get; init; }
}
