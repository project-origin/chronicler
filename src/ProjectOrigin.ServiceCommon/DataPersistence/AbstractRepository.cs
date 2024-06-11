using System.Data;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public abstract class AbstractRepository
{
    private readonly IDbTransaction _transaction;

    protected IDbConnection Connection => this._transaction?.Connection
        ?? throw new InvalidOperationException("Transaction is closed and no longer valid.");

    public AbstractRepository(IDbTransaction transaction) => this._transaction = transaction;
}
