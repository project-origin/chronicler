using System.Data;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
