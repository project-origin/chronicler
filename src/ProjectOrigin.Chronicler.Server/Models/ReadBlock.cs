using System;

namespace ProjectOrigin.Chronicler.Server.Models;

public record ReadBlock
{
    public required string RegistryName { get; init; }
    public required int BlockHeight { get; init; }
    public required DateTimeOffset ReadAt { get; init; }
}
