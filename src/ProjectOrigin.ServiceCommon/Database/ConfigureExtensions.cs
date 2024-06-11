using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using ProjectOrigin.ServiceCommon.Database.Postgres;
using ProjectOrigin.ServiceCommon.Otlp;
using Serilog;

namespace ProjectOrigin.ServiceCommon.Database;

public static class ConfigureExtensions
{
    public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration, Action<IDatabaseConfigurationBuilder> configuratorBuilder)
    {
        var builder = new DatabaseConfigurationBuilder();
        configuratorBuilder(builder);

        services.AddSingleton<IDatabaseUpgrader>(serviceProvider => ActivatorUtilities.CreateInstance<DatabaseUpgrader>(serviceProvider, builder.DatabaseScriptsAssemblies));

        services.AddScoped<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<IDatabaseFactory>().CreateConnection());
        services.AddScoped<IUnitOfWork>(serviceProvider => ActivatorUtilities.CreateInstance<UnitOfWork>(serviceProvider, builder.RepositoryFactories));

        services.ConfigurePostgres(configuration);

        var otlpOptions = configuration.GetSection(OtlpOptions.Prefix).Get<OtlpOptions>();
        if (otlpOptions != null && otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .WithTracing(provider => provider.AddNpgsql());
        }
    }

    public static IDatabaseUpgrader GetDatabaseUpgrader(this IConfiguration configuration, Serilog.ILogger logger, Action<IDatabaseConfigurationBuilder> options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSerilog(logger);
        services.ConfigureDatabase(configuration, options);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDatabaseUpgrader>();
    }
}
