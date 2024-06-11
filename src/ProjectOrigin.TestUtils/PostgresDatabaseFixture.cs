using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using ProjectOrigin.ServiceCommon.DataPersistence.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class PostgresDatabaseFixture<TScriptAssembly> : IAsyncLifetime
{
    public string HostConnectionString => _postgreSqlContainer.GetConnectionString();

    public string ContainerConnectionString
    {
        get
        {
            var properties = new Dictionary<string, string>
            {
                { "Host", _postgreSqlContainer.IpAddress },
                { "Port", PostgreSqlBuilder.PostgreSqlPort.ToString() },
                { "Database", "postgres" },
                { "Username", "postgres" },
                { "Password", "postgres" }
            };
            return string.Join(";", properties.Select(property => string.Join("=", property.Key, property.Value)));
        }
    }

    private PostgreSqlContainer _postgreSqlContainer;

    public PostgresDatabaseFixture()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:15")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        var mockLogger = new Mock<ILogger<PostgresUpgrader>>();
        var upgrader = new PostgresUpgrader(mockLogger.Object, Options.Create(new PostgresOptions
        {
            ConnectionString = _postgreSqlContainer.GetConnectionString()
        }), new List<Assembly> { typeof(TScriptAssembly).Assembly });
        await upgrader.Upgrade();
    }

    public async Task ResetDatabase()
    {
        var dataSource = NpgsqlDataSource.Create(_postgreSqlContainer.GetConnectionString());
        using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE blocks, transactions RESTART IDENTITY CASCADE;");
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }
}
