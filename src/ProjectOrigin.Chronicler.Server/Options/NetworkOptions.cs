using System.Collections.Generic;

namespace ProjectOrigin.Chronicler.Server.Options;

public record NetworkOptions
{
    public required IDictionary<string, RegistryInfo> Registries { get; init; }
}

public record RegistryInfo
{
    public required string Url { get; init; }
}
