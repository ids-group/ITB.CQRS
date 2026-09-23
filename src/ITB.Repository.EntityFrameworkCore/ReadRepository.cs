using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ITB.Domain.Interfaces;
using ITB.Repository.Abstraction;
using ITB.Specification;

namespace ITB.Repository.EntityFrameworkCore;

public class ReadRepository<TEntity>(DbContext dbContext, IEnumerable<IQueryableFilter<TEntity>> filters) : IReadRepository<TEntity>
    where TEntity : class
{
    protected readonly DbContext DbContext = dbContext;
    protected readonly DbSet<TEntity> DbSet = dbContext.Set<TEntity>();
    protected readonly IEnumerable<IQueryableFilter<TEntity>> Filters = filters;

    protected IQueryable<TEntity> BaseQuery
    {
        get
        {
            var query = DbSet.AsQueryable();

            if (Filters == null)
                return query;

            foreach (var filter in Filters)
            {
                query = filter.Apply(query);
            }

            return query;
        }
    }

    public async Task<TEntity> Find(params object[] keyObjects)
    {
        return await DbSet.FindAsync(keyObjects);
    }

    public IQueryable<TEntity> Query(Specification<TEntity> specification)
    {
        var query = BaseQuery;

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        return query.Where(specification);
    }

    public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> expression)
    {
        return BaseQuery.Where(expression);
    }

    public IQueryable<TEntity> Query()
    {
        return BaseQuery;
    }

    public async Task<TEntity> FirstOrDefault(Specification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery;

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        return await query.Where(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity> FirstOrDefault(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await BaseQuery.FirstOrDefaultAsync(expression, cancellationToken);
    }

    public async Task<int> Count(CancellationToken cancellationToken = default)
    {
        return await BaseQuery.CountAsync(cancellationToken);
    }

    public async Task<int> Count(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await BaseQuery.CountAsync(expression, cancellationToken);
    }

    public async Task<bool> Exists(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await BaseQuery.AnyAsync(expression, cancellationToken);
    }
}
