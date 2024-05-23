using System.Threading.Tasks;

namespace ProjectOrigin.Chronicler.Server.Database;

public interface IRepositoryUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
