using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

namespace ProjectOrigin.ServiceCommon.Logging;

public static class ConfigureExtensions
{
    public const string LogOutputFormatKey = "LogOutputFormat";

    public static ILogger GetSeriLogger(this IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Enrich.WithSpan();

        var logOutputFormat = configuration.GetValue<LogFormat>(LogOutputFormatKey);

        switch (logOutputFormat)
        {
            case LogFormat.Json:
                loggerConfiguration = loggerConfiguration.WriteTo.Console(new JsonFormatter());
                break;

            case LogFormat.Text:
                loggerConfiguration = loggerConfiguration.WriteTo.Console();
                break;

            default:
                throw new NotSupportedException($"{LogOutputFormatKey} of value ”{logOutputFormat}” is not supported");
        }

        return loggerConfiguration.CreateLogger();
    }
}
