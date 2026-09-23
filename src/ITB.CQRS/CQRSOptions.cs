using ITB.Shared.Result;
using Microsoft.Extensions.Logging;

namespace ITB.CQRS;

public class CQRSOptions
{
    public Func<Exception, ILogger, ExceptionFailure> ExceptionHandler { get; set; } = (exception, logger) =>
    {
        logger.LogError(exception, exception.Message);
        return new ExceptionFailure(exception);
    };
}
