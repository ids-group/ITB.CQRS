using ITB.Shared.Result;

namespace ITB.CQRS.Abstraction;

public interface IHandler<in TIn, TOut>
    where TIn : IRequest<TOut>
{
    Task<Result<TOut>> Handle(TIn input);
}

public interface IHandler<in TIn>
    where TIn : IRequest
{
    Task<Result> Handle(TIn input);
}
