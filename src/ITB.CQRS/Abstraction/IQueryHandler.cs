namespace ITB.CQRS.Abstraction;

public interface IQueryHandler<in TIn, TOut> : IHandler<TIn, TOut>
    where TIn : IQuery<TOut>
{
}
