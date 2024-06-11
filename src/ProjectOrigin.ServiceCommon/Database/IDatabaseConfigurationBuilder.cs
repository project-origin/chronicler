using System.Reflection;

namespace ProjectOrigin.ServiceCommon.Database;

public interface IDatabaseConfigurationBuilder
{
    public void AddRepository<TService, TImplementation>()
        where TService : class
        where TImplementation : AbstractRepository, TService;
    public void AddScriptsFromAssembly(Assembly assembly);
    public void AddScriptsFromAssemblyWithType<T>();
}
