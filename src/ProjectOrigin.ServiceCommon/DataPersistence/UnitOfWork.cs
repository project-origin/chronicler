using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IDbConnection _dbConnection;
    private readonly IDictionary<Type, ObjectFactory> _repositoryFactories;
    private Lazy<IDbTransaction> _transaction;
    private Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

    public UnitOfWork(IServiceProvider provider, IDbConnection dbConnection, IDictionary<Type, ObjectFactory> repositoryFactories)
    {
        _provider = provider;
        _dbConnection = dbConnection;
        _repositoryFactories = repositoryFactories;
        _transaction = new Lazy<IDbTransaction>(_dbConnection.BeginTransaction);
    }

    public T GetRepository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repository))
            return (T)repository;

        if (!_repositoryFactories.TryGetValue(typeof(T), out var factory))
            throw new InvalidOperationException($"No repository implementation found for {typeof(T).Name}");

        var repositoryInstance = factory(_provider, [_transaction.Value]) as T
            ?? throw new InvalidOperationException($"Failed to create repository instance for {typeof(T).Name}");

        _repositories.Add(typeof(T), repositoryInstance);
        return repositoryInstance;
    }

    public void Commit()
    {
        if (!_transaction.IsValueCreated)
            return;

        try
        {
            _transaction.Value.Commit();
        }
        catch
        {
            _transaction.Value.Rollback();
            throw;
        }

        ResetUnitOfWork();
    }

    public void Rollback()
    {
        if (!_transaction.IsValueCreated)
            return;

        _transaction.Value.Rollback();

        ResetUnitOfWork();
    }

    public void Dispose()
    {
        if (_transaction.IsValueCreated)
            _transaction.Value.Dispose();
    }

    private void ResetUnitOfWork()
    {
        if (_transaction.IsValueCreated)
            _transaction.Value.Dispose();

        _repositories.Clear();
        _transaction = new Lazy<IDbTransaction>(_dbConnection.BeginTransaction);
    }
}
