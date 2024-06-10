using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ProjectOrigin.ServiceCommon.DataPersistence.Postgres;

public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly PostgresOptions _databaseOptions;

    public PostgresConnectionFactory(IOptions<PostgresOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
        connection.Open();
        return connection;
    }
}
