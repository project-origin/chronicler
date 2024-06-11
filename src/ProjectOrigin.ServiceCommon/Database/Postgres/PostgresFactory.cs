using System.Data;
using DbUp;
using DbUp.Builder;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ProjectOrigin.ServiceCommon.Database.Postgres;

public class PostgresFactory : IDatabaseFactory
{
    private readonly PostgresOptions _databaseOptions;

    public PostgresFactory(IOptions<PostgresOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
        connection.Open();
        return connection;
    }

    public UpgradeEngineBuilder CreateUpgradeEngineBuilder()
    {
        return DeployChanges.To.PostgresqlDatabase(_databaseOptions.ConnectionString);
    }
}
