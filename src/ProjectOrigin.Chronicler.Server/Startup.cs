using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.Chronicler.Server.Database;
using ProjectOrigin.Chronicler.Server.Database.Postgres;
using ProjectOrigin.Chronicler.Server.Extensions;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.Chronicler.Server.Services;

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
        services.AddGrpc();

        services.ConfigurePersistance(_configuration);

        var otlpOptions = _configuration.GetSection(OtlpOptions.Prefix).GetValid<OtlpOptions>();
        if (otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    r.AddService("ProjectOrigin.Chronicler.Server",
                    serviceInstanceId: Environment.MachineName);
                })
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!))
                .WithTracing(provider =>
                    provider
                        .AddGrpcClientInstrumentation(grpcOptions =>
                        {
                            grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                                activity.SetTag("requestVersion", httpRequestMessage.Version);
                            grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                                activity.SetTag("responseVersion", httpResponseMessage.Version);
                            grpcOptions.SuppressDownstreamInstrumentation = true;
                        })
                        .AddAspNetCoreInstrumentation()
                        .AddNpgsql()
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!));
        }

        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

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
