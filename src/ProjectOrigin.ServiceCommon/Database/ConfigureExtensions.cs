using System.ComponentModel.DataAnnotations;
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
    public static void ConfigurePostgresPersistence(this IServiceCollection services, IConfiguration configuration, Action<IDatabaseConfigurationBuilder> options)
    {
        var builder = new PostgresConfigurationBuilder();
        options(builder);

        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new ValidationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDatabaseUpgrader>(serviceProvider => ActivatorUtilities.CreateInstance<PostgresUpgrader>(serviceProvider, builder.DatabaseScriptsAssemblies));
        services.AddSingleton<IDatabaseConnectionFactory, PostgresConnectionFactory>();

        services.AddScoped<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<IDatabaseConnectionFactory>().CreateConnection());
        services.AddScoped<IUnitOfWork>(serviceProvider => ActivatorUtilities.CreateInstance<UnitOfWork>(serviceProvider, builder.RepositoryFactories));

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
        services.ConfigurePostgresPersistence(configuration, options);
        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDatabaseUpgrader>();
    }
}
