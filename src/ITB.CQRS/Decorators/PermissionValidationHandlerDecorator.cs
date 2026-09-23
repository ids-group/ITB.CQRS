using ITB.CQRS.Abstraction;
using ITB.CQRS.Models;
using ITB.Shared.Result;

namespace ITB.CQRS.Decorators;

public class PermissionValidationHandlerDecorator<TIn, TOut>(IHandler<TIn, TOut> decorated, IEnumerable<IPermissionValidator<TIn>> permissionValidators) : HandlerDecoratorBase<TIn, TOut>(decorated)
    where TIn : IRequest<TOut>
{
    private readonly IEnumerable<IPermissionValidator<TIn>> _permissionValidators = permissionValidators;

    public override async Task<Result<TOut>> Handle(TIn input)
    {
        foreach (var permissionValidator in _permissionValidators)
        {
            var result = await permissionValidator.Validate(input);

            if (!result.IsValid)
            {
                return Result.Forbidden(result.ToString());
            }
        }

        return await Decorated.Handle(input);
    }
}

public class PermissionValidationHandlerDecorator<TIn>(IHandler<TIn> decorated, IEnumerable<IPermissionValidator<TIn>> permissionValidators) : HandlerDecoratorBase<TIn>(decorated)
    where TIn : IRequest
{
    private readonly IEnumerable<IPermissionValidator<TIn>> _permissionValidators = permissionValidators;

    public override async Task<Result> Handle(TIn input)
    {
        foreach (var permissionValidator in _permissionValidators)
        {
            var result = await permissionValidator.Validate(input);

            if (!result.IsValid)
            {
                return Result.Forbidden(result.ToString());
            }
        }

        return await Decorated.Handle(input);
    }
}
