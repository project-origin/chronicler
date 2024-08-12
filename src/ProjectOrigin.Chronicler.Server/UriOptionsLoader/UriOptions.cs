using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public record UriOptions
{
    [Required(AllowEmptyStrings = false)]
    public required Uri ConfigurationUri { get; init; }

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(15);
}
