using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ProjectOrigin.ServiceCommon.Extensions;

public static class IConfigurationExtensions
{
    public static T GetValid<T>(this IConfiguration configuration) where T : IValidatableObject
    {
        try
        {
            var value = configuration.Get<T>();

            if (value is null)
                throw new ArgumentNullException($"Configuration value of type {typeof(T)} is null");

            Validator.ValidateObject(value, new ValidationContext(value), true);

            return value;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to convert configuration value"))
        {
            throw new ValidationException($"Configuration value of type {typeof(T)} is invalid", ex);
        }
    }

    public static WebApplication BuildApp<T>(this IConfigurationRoot configuration)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration, shouldDisposeConfiguration: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        var startup = Activator.CreateInstance(typeof(T), builder.Configuration) as dynamic;
        startup!.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, builder.Environment);

        return app;
    }
}
