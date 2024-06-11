using System.Data;

namespace ProjectOrigin.ServiceCommon.DataPersistence;

public interface IUnitOfWork
{
    void Commit();
    void Rollback();
    T GetRepository<T>() where T : class;
}
