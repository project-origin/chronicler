using System.Data;

namespace ProjectOrigin.ServiceCommon.Database;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
}
