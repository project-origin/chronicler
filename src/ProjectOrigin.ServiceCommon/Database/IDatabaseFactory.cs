using System.Data;
using DbUp.Builder;

namespace ProjectOrigin.ServiceCommon.Database;

public interface IDatabaseFactory
{
    IDbConnection CreateConnection();
    UpgradeEngineBuilder CreateUpgradeEngineBuilder();
}
