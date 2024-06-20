using System.Collections.Generic;

namespace ProjectOrigin.Chronicler.Server.Options;

public class RegistryOptions
{
    public Dictionary<string, string> RegistryUrls { get; set; } = new Dictionary<string, string>();
}
