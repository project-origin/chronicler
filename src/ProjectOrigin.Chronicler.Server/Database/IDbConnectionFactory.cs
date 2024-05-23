using System.Data;

namespace ProjectOrigin.Chronicler.Server.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
