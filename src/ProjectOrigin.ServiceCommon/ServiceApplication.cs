using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.ServiceCommon.DataPersistence;
using ProjectOrigin.ServiceCommon.Extensions;
using ProjectOrigin.ServiceCommon.Logging;
using Serilog;

namespace ProjectOrigin.ServiceCommon;

public class ServiceApplication<TStartup>
{
    private string? _migration;
    private string? _serve;

    public ServiceApplication<TStartup> ConfigureMigration(string argFlag)
    {
        _migration = argFlag;
        return this;
    }

    public ServiceApplication<TStartup> ConfigureWebApplication(string argFlag)
    {
        _serve = argFlag;
        return this;
    }

    public async Task RunAsync(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        Log.Logger = configuration.GetSeriLogger();

        try
        {
            Log.Information("Application starting.");

            if (_migration != null && args.Contains(_migration))
            {
                await RunDatabaseMigration(configuration);
            }

            if (_serve != null && args.Contains(_serve))
            {
                await RunServe(configuration);
            }

            Log.Information("Application closing.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            Environment.ExitCode = -1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task RunDatabaseMigration(IConfigurationRoot configuration)
    {
        Log.Information("Starting repository migration.");

        await configuration.GetDatabaseUpgrader(Log.Logger, (options) =>
        {
            options.AddScriptsFromAssemblyWithType<TStartup>();
        }).Upgrade();

        Log.Information("Repository migrated successfully.");
    }

    private static async Task RunServe(IConfigurationRoot configuration)
    {
        Log.Information("Starting server.");

        WebApplication app = configuration.BuildApp<TStartup>();

        var upgrader = app.Services.GetService<IDatebaseUpgrader>();
        if (upgrader != null && await upgrader.IsUpgradeRequired())
            throw new InvalidOperationException("Repository is not up to date. Please run with --migrate first.");

        await app.RunAsync();

        Log.Information("Server stopped.");
    }

}
