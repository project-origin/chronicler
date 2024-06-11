namespace ProjectOrigin.ServiceCommon.DataPersistence;

public interface IDatebaseUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
