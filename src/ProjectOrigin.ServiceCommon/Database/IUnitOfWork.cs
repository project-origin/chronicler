namespace ProjectOrigin.ServiceCommon.Database;

public interface IUnitOfWork
{
    void Commit();
    void Rollback();
    T GetRepository<T>() where T : class;
}
