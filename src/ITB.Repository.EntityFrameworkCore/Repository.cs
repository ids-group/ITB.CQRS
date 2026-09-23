using ITB.Domain.Entities;
using ITB.Domain.Interfaces;
using ITB.Repository.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace ITB.Repository.EntityFrameworkCore;

public class Repository<TEntity>(DbContext dbContext, IEnumerable<IQueryableFilter<TEntity>> filters) : ReadRepository<TEntity>(dbContext, filters), IRepository<TEntity>
    where TEntity : class, IEntity
{
    public async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        return (await DbSet.AddAsync(entity, cancellationToken)).Entity;
    }

    public async Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Attach(entity);
        return await Task.FromResult(DbSet.Update(entity).Entity);
    }

    public virtual async Task<bool> Delete(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return await Task.FromResult(true);
    }
}
