using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectOrigin.ServiceCommon.Extensions;

namespace ProjectOrigin.ServiceCommon.Otlp;

public static class ConfigureExtensions
{
    public static void ConfigureDefaultOtlp(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpOptions = configuration.GetSection(OtlpOptions.Prefix).Get<OtlpOptions>();
        if (otlpOptions != null && otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    string assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidOperationException("Failed to get entry assembly name");
                    r.AddService(assemblyName, serviceInstanceId: Environment.MachineName);
                })
                .WithMetrics(metrics =>
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!))
                .WithTracing(provider =>
                    provider
                        .AddAspNetCoreInstrumentation()
                        .AddOtlpExporter(o => o.Endpoint = otlpOptions.Endpoint!));
        }
    }
}
