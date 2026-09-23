using ITB.CQRS.Abstraction;
using ITB.CQRS.Decorators;
using ITB.Shared.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ITB.CQRS.UnitTests.Tests;

public class ErrorHandlerDecoratorTests
{
    [Fact]
    public async Task An_unhandled_exception_becomes_an_ExceptionFailure_by_default()
    {
        var decorator = Decorate(new CQRSOptions());

        var result = await decorator.Handle(new Ping());

        Assert.IsType<ExceptionFailure>(result.Failure);
    }

    [Fact]
    public async Task The_exception_handler_can_map_an_exception_to_any_failure()
    {
        var options = new CQRSOptions
        {
            ExceptionHandler = (exception, _) => exception is KeyNotFoundException
                ? new NotFoundFailure(exception.Message)
                : new ExceptionFailure(exception),
        };

        var result = await Decorate(options).Handle(new Ping());

        var failure = Assert.IsType<NotFoundFailure>(result.Failure);
        Assert.Equal("missing", failure.Message);
    }

    [Fact]
    public async Task The_exception_handler_applies_to_result_less_commands_too()
    {
        var options = new CQRSOptions { ExceptionHandler = (exception, _) => new ConflictFailure(exception.Message) };
        var decorator = new ErrorHandlerDecorator<PingCommand>(
            new ThrowingCommandHandler(),
            Options.Create(options),
            NullLogger<ErrorHandlerDecorator<PingCommand>>.Instance);

        var result = await decorator.Handle(new PingCommand());

        Assert.IsType<ConflictFailure>(result.Failure);
    }

    [Fact]
    public async Task A_handler_typed_to_return_ExceptionFailure_still_assigns()
    {
        // 2.0.0 typed the handler as Func<Exception, ILogger, ExceptionFailure>. Func is covariant in its
        // result, so a delegate written against that signature still assigns without a change.
        Func<Exception, ILogger, ExceptionFailure> legacy = (exception, _) => new ExceptionFailure(exception);
        var options = new CQRSOptions { ExceptionHandler = legacy };

        var result = await Decorate(options).Handle(new Ping());

        Assert.IsType<ExceptionFailure>(result.Failure);
    }

    private static ErrorHandlerDecorator<Ping, string> Decorate(CQRSOptions options) =>
        new(new ThrowingHandler(), Options.Create(options), NullLogger<ErrorHandlerDecorator<Ping, string>>.Instance);

    public sealed class Ping : QueryBase<string>;

    public sealed class PingCommand : CommandBase;

    private sealed class ThrowingHandler : IHandler<Ping, string>
    {
        public Task<Result<string>> Handle(Ping input) => throw new KeyNotFoundException("missing");
    }

    private sealed class ThrowingCommandHandler : IHandler<PingCommand>
    {
        public Task<Result> Handle(PingCommand input) => throw new InvalidOperationException("taken");
    }
}
