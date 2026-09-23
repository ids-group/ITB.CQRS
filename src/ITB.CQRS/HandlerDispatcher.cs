using ITB.CQRS.Abstraction;
using ITB.Shared.Result;
using Microsoft.Extensions.DependencyInjection;

namespace ITB.CQRS;

public class HandlerDispatcher(IServiceProvider serviceProvider) : IHandlerDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<Result> Handle<TIn>(TIn input)
        where TIn : CommandBase
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var handler = _serviceProvider.GetRequiredService<IHandler<TIn>>();

        return await handler.Handle(input);
    }

    public async Task<Result<TOut>> Handle<TIn, TOut>(TIn input)
        where TIn : IRequest<TOut>
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var handler = _serviceProvider.GetRequiredService<IHandler<TIn, TOut>>();

        return await handler.Handle(input);
    }
}
