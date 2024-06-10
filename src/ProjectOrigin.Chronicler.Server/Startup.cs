using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.Chronicler.Server.Services;
using ProjectOrigin.ServiceCommon.DataPersistence;
using ProjectOrigin.ServiceCommon.Grpc;
using ProjectOrigin.ServiceCommon.Otlp;
using ProjectOrigin.WalletSystem.Server.Repositories;

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
        services.ConfigurePostgresPersistence(_configuration, Assembly.GetExecutingAssembly());
        services.ConfigureGrpc(_configuration);

        services.AddScoped<IChroniclerRepository, ChroniclerRepository>();
        services.AddOptions<ChroniclerOptions>()
            .BindConfiguration(ChroniclerOptions.SectionPrefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();
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
