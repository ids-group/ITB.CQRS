using ITB.CQRS.Abstraction;
using ITB.Shared.Result;

namespace ITB.CQRS;

public interface IHandlerDispatcher
{
    Task<Result<TOut>> Handle<TIn, TOut>(TIn input)
        where TIn : IRequest<TOut>;

    Task<Result> Handle<TIn>(TIn input)
        where TIn : CommandBase;
}
