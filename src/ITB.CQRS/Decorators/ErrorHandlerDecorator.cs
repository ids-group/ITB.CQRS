using ITB.CQRS.Abstraction;
using ITB.Shared.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITB.CQRS.Decorators;

public class ErrorHandlerDecorator<TIn, TOut>(IHandler<TIn, TOut> decorated, IOptions<CQRSOptions> options, ILogger<ErrorHandlerDecorator<TIn, TOut>> logger) : HandlerDecoratorBase<TIn, TOut>(decorated)
    where TIn : IRequest<TOut>
{
    private readonly CQRSOptions _options = options.Value;
    private readonly ILogger _logger = logger;

    public override async Task<Result<TOut>> Handle(TIn input)
    {
        try
        {
            return await Decorated.Handle(input);
        }
        catch (Exception ex)
        {
            return _options.ExceptionHandler(ex, _logger);
        }
    }
}

public class ErrorHandlerDecorator<TIn>(IHandler<TIn> decorated, IOptions<CQRSOptions> options, ILogger<ErrorHandlerDecorator<TIn>> logger) : HandlerDecoratorBase<TIn>(decorated)
    where TIn : IRequest
{
    private readonly CQRSOptions _options = options.Value;
    private readonly ILogger _logger = logger;

    public override async Task<Result> Handle(TIn input)
    {
        try
        {
            return await Decorated.Handle(input);
        }
        catch (Exception ex)
        {
            return _options.ExceptionHandler(ex, _logger);
        }
    }
}
