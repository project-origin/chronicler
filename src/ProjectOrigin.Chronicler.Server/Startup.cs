using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.Chronicler.Server.Services;
using ProjectOrigin.ServiceCommon.Database;
using ProjectOrigin.ServiceCommon.Grpc;
using ProjectOrigin.ServiceCommon.Otlp;
using ProjectOrigin.Chronicler.Server.Repositories;
using Microsoft.Extensions.Options;
using ProjectOrigin.ServiceCommon.UriOptionsLoader;
using ProjectOrigin.Chronicler.Server.BlockReader;
using ProjectOrigin.ServiceCommon.Database.Postgres;

namespace ProjectOrigin.Chronicler.Server;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureDefaultOtlp(_configuration);
        services.ConfigureGrpc(_configuration);
        services.ConfigureDatabase(_configuration, (options) =>
        {
            options.AddScriptsFromAssemblyWithType<Startup>();
            options.AddRepository<IChroniclerRepository, ChroniclerRepository>();
        });

        services.AddSingleton<IRegistryClientFactory, RegistryClientFactory>();
        services.AddTransient<IRegistryService, RegistryService>();

        services.AddOptions<ChroniclerOptions>()
            .BindConfiguration(ChroniclerOptions.SectionPrefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<BlockReaderBackgroundService>();
        services.AddTransient<IBlockReaderJob, BlockReaderJob>();

        services.AddHttpClient();
        services.ConfigureUriOptionsLoader<NetworkOptions>("network");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<ChroniclerService>();
            endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    }
}
