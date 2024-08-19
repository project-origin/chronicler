using System.Collections.Generic;

namespace ProjectOrigin.Chronicler.Server.Options;

public record NetworkOptions
{
    public required IDictionary<string, string> RegistryUrls { get; init; }
}
