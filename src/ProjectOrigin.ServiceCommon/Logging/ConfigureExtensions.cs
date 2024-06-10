using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

namespace ProjectOrigin.ServiceCommon.Logging;

public static class ConfigureExtensions
{
    public static ILogger GetSeriLogger(this IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Enrich.WithSpan();

        var logOutputFormat = configuration.GetValue<string>("LogOutputFormat");

        switch (logOutputFormat)
        {
            case "json":
                loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());
                break;

            case "text":
                loggerConfiguration = loggerConfiguration.WriteTo.Console();
                break;

            default:
                throw new NotSupportedException($"LogOutputFormat of value ”{logOutputFormat}” is not supported");
        }

        return loggerConfiguration.CreateLogger();
    }
}
