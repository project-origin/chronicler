using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectOrigin.ServiceCommon.Database.Postgres;

public static class ConfigureExtensions
{
    public static void ConfigurePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new ValidationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDatabaseFactory, PostgresFactory>();
    }
}
