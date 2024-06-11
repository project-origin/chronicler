using System.Threading.Tasks;
using OpenTelemetry;
using ProjectOrigin.Chronicler.Server.Models;

namespace ProjectOrigin.WalletSystem.Server.Repositories;

public interface IChroniclerRepository
{
    Task InsertClaimIntent(ClaimIntent certificate);
}
