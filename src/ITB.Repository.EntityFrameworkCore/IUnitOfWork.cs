using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ITB.Repository.EntityFrameworkCore;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChanges(CancellationToken cancellationToken = default);
    void DetachAllEntities();
    void SetCommandTimeout(TimeSpan timeSpan);
    void AttachAsUnchangedIfMissing<TEntity>(TEntity entity) where TEntity : class;
    DbContext Context();
    Task<IDbContextTransaction> BeginTransaction();
    Task CommitTransaction();
    Task RollbackTransaction();
}
