namespace ITB.CQRS.Abstraction;

public interface ICommand<TOut> : IRequest<TOut>
{
}

public interface ICommand : IRequest
{
}
