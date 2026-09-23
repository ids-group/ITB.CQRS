using System.Linq.Expressions;

namespace ITB.Repository.Abstraction;

public delegate Task<TEntity?> FindOne<TEntity>(Expression<Func<TEntity, bool>> predicate);

public delegate Task<TResult?> FindOne<TEntity, TResult>(
    Expression<Func<TEntity, bool>> predicate,
    Expression<Func<TEntity, TResult>> selector)
    where TResult : class;

public delegate Task<List<TEntity>> FindMany<TEntity>(Expression<Func<TEntity, bool>> predicate);

public delegate Task<IEnumerable<TResult>> FindMany<TEntity, TResult>(
    Expression<Func<TEntity, bool>> predicate,
    Expression<Func<TEntity, TResult>> selector)
    where TResult : class;
