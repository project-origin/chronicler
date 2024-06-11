using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using ProjectOrigin.ServiceCommon.DataPersistence.Postgres;
using ProjectOrigin.ServiceCommon.Otlp;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public static class ConfigureExtensions
{
    public static void ConfigurePostgresPersistence(this IServiceCollection services, IConfiguration configuration, Action<IDataPersistenceConfigurationBuilder> options)
    {
        var builder = new PostgresConfigurationBuilder();
        options(builder);

        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new ValidationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDatebaseUpgrader>(serviceProvider => ActivatorUtilities.CreateInstance<PostgresUpgrader>(serviceProvider, builder.DatabaseScriptsAssemblies));
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

        services.AddScoped<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<IDbConnectionFactory>().CreateConnection());
        services.AddScoped<IUnitOfWork>(serviceProvider => ActivatorUtilities.CreateInstance<UnitOfWork>(serviceProvider, builder.RepositoryFactories));

        var otlpOptions = configuration.GetSection(OtlpOptions.Prefix).Get<OtlpOptions>();
        if (otlpOptions != null && otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .WithTracing(provider => provider.AddNpgsql());
        }
    }
}
