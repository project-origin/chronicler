namespace ProjectOrigin.ServiceCommon.DataPersistence;

public interface IRepositoryUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
