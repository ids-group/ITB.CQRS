using ITB.CQRS.Abstraction;
using ITB.CQRS.Models;
using ITB.Domain.Entities;
using ITB.Repository.Abstraction;
using ITB.Shared.Result;
using ITB.Specification;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ITB.CQRS;

public abstract class GetFilteredListQuery<TOut> : FilterBase, IQuery<PagedList<TOut>>
{
}

public abstract class GetFilteredListQueryHandler<TIn, TOut, TEntity>(IReadRepository<TEntity> repository)
    : QueryHandlerBase<TIn, PagedList<TOut>>
    where TIn : GetFilteredListQuery<TOut>
    where TEntity : class, IEntity
{
    protected readonly IReadRepository<TEntity> Repository = repository;
    protected Specification<TEntity> Specification { get; set; } = new DefaultSpecification<TEntity>();

    // Override to reject the request before any query runs. A failed Result is returned to the caller as-is.
    protected virtual Task<Result> CheckAccess(TIn input) => Task.FromResult(Result.Success());

    // Only called once CheckAccess has passed, so an override does not need to re-check permissions.
    protected virtual Task<IQueryable<TEntity>> BuildBaseQuery(TIn input)
        => Task.FromResult(Repository.Query(Specification));

    protected virtual Task BuildSpecification(TIn input) => Task.CompletedTask;

    protected virtual IQueryable<TEntity> Search(IQueryable<TEntity> query, string searchText) => query;

    protected virtual IQueryable<TEntity> Sort(IQueryable<TEntity> query, Sorting sorting) => query;

    protected virtual async Task<List<TOut>> GetItems(IQueryable<TEntity> pagedQuery, TIn input)
        => await pagedQuery.ProjectToType<TOut>().ToListAsync();

    public override async Task<Result<PagedList<TOut>>> Handle(TIn input)
    {
        var accessResult = await CheckAccess(input);
        if (!accessResult.IsSuccess)
        {
            return accessResult.Failure;
        }

        await BuildSpecification(input);

        var query = await BuildBaseQuery(input);

        query = Search(query, input.SearchText);
        var totalCount = await query.CountAsync();
        query = Sort(query, input.Sorting);
        query = query.ApplyPageFilter(input);

        var items = await GetItems(query, input);
        return new PagedList<TOut>(items, totalCount);
    }
}
