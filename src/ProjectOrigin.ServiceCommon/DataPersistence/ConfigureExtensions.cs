using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ProjectOrigin.ServiceCommon.DataPersistence.Postgres;
using ProjectOrigin.ServiceCommon.Extensions;
using ProjectOrigin.ServiceCommon.Otlp;


namespace ProjectOrigin.ServiceCommon.DataPersistence;

public static class ConfigureExtensions
{
    public static void ConfigurePostgresPersistence(this IServiceCollection services, IConfiguration configuration, Assembly? databaseScriptsAssembly = null)
    {
        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new ValidationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRepositoryUpgrader>(s =>
            new PostgresUpgrader(
                s.GetRequiredService<ILogger<PostgresUpgrader>>(),
                s.GetRequiredService<IOptions<PostgresOptions>>(),
                databaseScriptsAssembly));
        services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDbTransaction>(s => s.GetRequiredService<IUnitOfWork>().Transaction);

        services.AddScoped<IDbConnection>(serviceProvider => serviceProvider.GetRequiredService<IDbConnectionFactory>().CreateConnection());

        var otlpOptions = configuration.GetSection(OtlpOptions.Prefix).Get<OtlpOptions>();
        if (otlpOptions != null && otlpOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .WithTracing(provider => provider.AddNpgsql());
        }
    }
}
