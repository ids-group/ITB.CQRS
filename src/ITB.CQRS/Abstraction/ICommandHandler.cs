namespace ITB.CQRS.Abstraction;

public interface ICommandHandler<in TIn, TOut> : IHandler<TIn, TOut>
    where TIn : ICommand<TOut>
{
}

public interface ICommandHandler<in TIn> : IHandler<TIn>
    where TIn : ICommand
{
}
