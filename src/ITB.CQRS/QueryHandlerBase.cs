using ITB.CQRS.Abstraction;
using ITB.Shared.Result;

namespace ITB.CQRS;

public abstract class QueryHandlerBase<TIn, TOut> : IQueryHandler<TIn, TOut>
    where TIn : IQuery<TOut>
{
    public abstract Task<Result<TOut>> Handle(TIn input);
}
