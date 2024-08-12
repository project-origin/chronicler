using Microsoft.Extensions.Options;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public class UriOptionsConfigure<TOptions> : IConfigureOptions<TOptions> where TOptions : class
{
    private readonly UriOptionsLoaderService<TOptions> _httpOptionsLoader;

    public UriOptionsConfigure(UriOptionsLoaderService<TOptions> httpOptionsLoader)
    {
        _httpOptionsLoader = httpOptionsLoader;
    }

    public void Configure(TOptions options)
    {
        _httpOptionsLoader.Configure(options);
    }
}
