using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Chronicler.Server.Database;
using ProjectOrigin.Chronicler.Server.Database.Postgres;

namespace ProjectOrigin.Chronicler.Server.Extensions;

public static class IServiceCollectionExtensions
{
    public static void ConfigurePersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IRepositoryUpgrader, PostgresUpgrader>();
        services.AddOptions<PostgresOptions>()
            .Configure(x => x.ConnectionString = configuration.GetConnectionString("Database")
                ?? throw new ValidationException("Configuration does not contain a connection string named 'Database'."))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
