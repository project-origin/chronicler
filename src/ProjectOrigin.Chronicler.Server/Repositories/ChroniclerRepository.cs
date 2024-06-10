using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectOrigin.Chronicler.Server.Models;
using ProjectOrigin.ServiceCommon.DataPersistence;
using ProjectOrigin.WalletSystem.Server.Repositories;
public class ChroniclerRepository : AbstractRepository, IChroniclerRepository
{
    public ChroniclerRepository(IDbTransaction transaction) : base(transaction) { }

    public Task InsertClaimIntent(ClaimIntent certificate)
    {
        return Connection.ExecuteAsync(
            @"INSERT INTO claim_intents(id, registry_name, certificate_id, quantity, random_r, commitment)
              VALUES (@id, @registryName, @certificateId, @quantity, @randomR, @commitment)",
            certificate);
    }
}
