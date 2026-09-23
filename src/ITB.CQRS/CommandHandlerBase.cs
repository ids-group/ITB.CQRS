using ITB.CQRS.Abstraction;
using ITB.Shared.Result;

namespace ITB.CQRS;

public abstract class CommandHandlerBase<TIn, TOut> : ICommandHandler<TIn, TOut>
    where TIn : ICommand<TOut>
{
    public abstract Task<Result<TOut>> Handle(TIn input);
}

public abstract class CommandHandlerBase<TIn> : ICommandHandler<TIn>
    where TIn : ICommand
{
    public abstract Task<Result> Handle(TIn input);
}
