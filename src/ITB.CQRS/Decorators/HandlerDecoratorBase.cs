using ITB.CQRS.Abstraction;
using ITB.Shared.Result;

namespace ITB.CQRS.Decorators;

public abstract class HandlerDecoratorBase<TIn, TOut>(IHandler<TIn, TOut> decorated) : IHandler<TIn, TOut>
    where TIn : IRequest<TOut>
{
    protected IHandler<TIn, TOut> Decorated { get; } = decorated;

    public abstract Task<Result<TOut>> Handle(TIn input);
}

public abstract class HandlerDecoratorBase<TIn>(IHandler<TIn> decorated) : IHandler<TIn>
    where TIn : IRequest
{
    protected IHandler<TIn> Decorated { get; } = decorated;

    public abstract Task<Result> Handle(TIn input);
}
