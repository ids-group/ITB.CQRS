using System.Linq.Expressions;
using ITB.Specification;

namespace ITB.Repository.Abstraction;

public interface IRepository
{
}

public interface IReadRepository<TEntity> : IRepository
    where TEntity : class
{
    Task<TEntity> Find(params object[] keyObjects);
    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> expression);
    IQueryable<TEntity> Query(Specification<TEntity> specification);
    IQueryable<TEntity> Query();
    Task<TEntity> FirstOrDefault(Specification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TEntity> FirstOrDefault(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task<int> Count(CancellationToken cancellationToken = default);
    Task<int> Count(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task<bool> Exists(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class
{
    Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> Delete(TEntity entity, CancellationToken cancellationToken = default);
}
