using System;

namespace ProjectOrigin.Chronicler.Models;

public record LastReadBlock
{
    public required string RegistryName { get; init; }
    public required int BlockHeight { get; init; }
    public required DateTimeOffset ReadAt { get; init; }
}
