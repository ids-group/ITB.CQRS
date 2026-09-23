using FluentValidation;
using ITB.CQRS.Abstraction;
using ITB.Shared.Result;
using ValidationError = ITB.Shared.Result.ValidationError;

namespace ITB.CQRS.Decorators;

public class ValidationHandlerDecorator<TIn, TOut>(IHandler<TIn, TOut> decorated, IEnumerable<IValidator<TIn>> validators) : HandlerDecoratorBase<TIn, TOut>(decorated)
    where TIn : IRequest<TOut>
{
    private readonly IEnumerable<IValidator<TIn>> _validators = validators;

    public override async Task<Result<TOut>> Handle(TIn input)
    {
        var context = new ValidationContext<TIn>(input);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            return Result.ValidationError(failures.Select(f => new ValidationError { Field = f.PropertyName, Message = f.ErrorMessage }).ToArray());
        }

        return await Decorated.Handle(input);
    }
}

public class ValidationHandlerDecorator<TIn>(IHandler<TIn> decorated, IEnumerable<IValidator<TIn>> validators) : HandlerDecoratorBase<TIn>(decorated)
    where TIn : IRequest
{
    private readonly IEnumerable<IValidator<TIn>> _validators = validators;

    public override async Task<Result> Handle(TIn input)
    {
        var context = new ValidationContext<TIn>(input);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            return Result.ValidationError(failures.Select(f => new ValidationError { Field = f.PropertyName, Message = f.ErrorMessage }).ToArray());
        }

        return await Decorated.Handle(input);
    }
}
