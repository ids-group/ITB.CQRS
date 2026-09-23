namespace ITB.Domain.Interfaces;

// An ambient filter applied to every repository query for TEntity, such as tenant isolation or soft-delete.
// Registered per entity and composed by the repository.
public interface IQueryableFilter<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);
}
