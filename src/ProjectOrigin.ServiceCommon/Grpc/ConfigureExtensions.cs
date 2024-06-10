using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using ProjectOrigin.ServiceCommon.Otlp;

namespace ProjectOrigin.ServiceCommon.Grpc;

public static class ConfigureExtensions
{
    public static void ConfigureGrpc(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc();

        var otlpOptions = configuration.GetSection(OtlpOptions.Prefix).Get<OtlpOptions>();
        if (otlpOptions != null && otlpOptions.Enabled)
        {
            services.AddOpenTelemetry().WithTracing(tracing =>
                tracing
                    .AddGrpcClientInstrumentation(grpcOptions =>
                    {
                        grpcOptions.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            activity.SetTag("requestVersion", httpRequestMessage.Version);
                        grpcOptions.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                            activity.SetTag("responseVersion", httpResponseMessage.Version);
                        grpcOptions.SuppressDownstreamInstrumentation = true;
                    }));
        }
    }
}
