using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public static class ConfigureExtensions
{
    /// <summary>
    /// Configures the UriOptionsLoaderService to load the Uri json object into the specified type
    /// </summary>
    /// <typeparam name="TOption">The type to load the Uri json object into</typeparam>
    /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection.</param>
    /// <param name="configSectionPath">The name of the configuration section to bind configuration from</param>
    public static void ConfigureUriOptionsLoader<TOption>(this IServiceCollection services, string configSectionPath) where TOption : class
    {
        services.AddOptions<UriOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<TOption>, UriOptionsConfigure<TOption>>();
        services.AddSingleton<IOptionsChangeTokenSource<TOption>, UriOptionsChangeTokenSource<TOption>>();
        services.AddSingleton<UriOptionsLoaderService<TOption>>();
        services.AddHostedService(provider => provider.GetRequiredService<UriOptionsLoaderService<TOption>>());
    }
}
