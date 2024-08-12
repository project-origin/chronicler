using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public class UriOptionsChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions> where TOptions : class
{
    private readonly UriOptionsLoaderService<TOptions> _httpOptionsLoader;

    public UriOptionsChangeTokenSource(UriOptionsLoaderService<TOptions> httpOptionsLoader)
    {
        _httpOptionsLoader = httpOptionsLoader;
    }

    public string? Name => null;

    public IChangeToken GetChangeToken()
    {
        return _httpOptionsLoader.OptionChangeToken;
    }
}
