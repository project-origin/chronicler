using System.Data;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();
    private readonly IDbConnection _connection;
    private readonly IServiceProvider _provider;
    private Lazy<IDbTransaction> _lazyTransaction;

    public UnitOfWork(IDbConnection connection, IServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
        _lazyTransaction = new Lazy<IDbTransaction>(_connection.BeginTransaction);
    }

    public T GetRepository<T>() where T : class
    {
        var repositoryType = typeof(T);

        if (_repositories.TryGetValue(repositoryType, out var foundRepository))
        {
            return (T)foundRepository;
        }
        else
        {
            var newRepository = Activator.CreateInstance(repositoryType, _lazyTransaction.Value) as T
                ?? throw new InvalidOperationException("Repository could not be created.");

            _repositories.Add(repositoryType, newRepository);
            return newRepository;
        }
    }

    public void Commit()
    {
        if (!_lazyTransaction.IsValueCreated)
            return;

        try
        {
            _lazyTransaction.Value.Commit();
        }
        catch
        {
            _lazyTransaction.Value.Rollback();
            throw;
        }
        finally
        {
            ResetUnitOfWork();
        }
    }

    public void Rollback()
    {
        if (!_lazyTransaction.IsValueCreated)
            return;

        _lazyTransaction.Value.Rollback();

        ResetUnitOfWork();
    }

    private void ResetUnitOfWork()
    {
        if (_lazyTransaction.IsValueCreated)
            _lazyTransaction.Value.Dispose();

        _lazyTransaction = new Lazy<IDbTransaction>(_connection.BeginTransaction);

        _repositories.Clear();
    }

    public void Dispose()
    {

        if (_lazyTransaction.IsValueCreated)
            _lazyTransaction.Value.Dispose();

        _repositories.Clear();
    }
}
