using System.Reflection;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public interface IDataPersistenceConfigurationBuilder
{
    public void AddRepository<TService, TImplementation>()
        where TService : class
        where TImplementation : AbstractRepository, TService;
    public void AddScriptsFromAssembly(Assembly assembly);
    public void AddScriptsFromAssemblyWithType<T>();
}
