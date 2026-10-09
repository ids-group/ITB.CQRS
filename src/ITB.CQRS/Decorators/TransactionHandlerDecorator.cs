using ITB.CQRS.Abstraction;
using ITB.Shared.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ITB.CQRS.Decorators;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreTransactionAttribute : Attribute
{
}

public class TransactionHandlerDecorator<TIn, TOut>(
    IHandler<TIn, TOut> decorated,
    DbContext dbContext,
    IEnumerable<ITransactionParticipant> participants,
    ILogger<TransactionHandlerDecorator<TIn, TOut>> logger) : HandlerDecoratorBase<TIn, TOut>(decorated)
    where TIn : CommandBase<TOut>
{
    private readonly DbContext _dbContext = dbContext;
    private readonly ITransactionParticipant[] _participants = participants.ToArray();
    private readonly ILogger _logger = logger;

    public override async Task<Result<TOut>> Handle(TIn input)
    {
        var ignoreAttribute = Attribute.GetCustomAttribute(input.GetType(), typeof(IgnoreTransactionAttribute));
        if (ignoreAttribute == null)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                Result<TOut> attempt;
                try
                {
                    attempt = await Decorated.Handle(input);

                    if (attempt.IsSuccess)
                    {
                        await transaction.CommitAsync();
                    }
                }
                catch
                {
                    await TransactionSignals.Abandon(transaction, _participants, _logger);
                    throw;
                }

                if (!attempt.IsSuccess)
                {
                    await TransactionSignals.Abandon(transaction, _participants, _logger);
                }

                return attempt;
            });

            if (result.IsSuccess)
            {
                await TransactionSignals.Notify(_participants, p => p.Committed(), nameof(ITransactionParticipant.Committed), _logger);
            }

            return result;
        }

        return await Decorated.Handle(input);
    }
}

public class TransactionHandlerDecorator<TIn>(
    IHandler<TIn> decorated,
    DbContext dbContext,
    IEnumerable<ITransactionParticipant> participants,
    ILogger<TransactionHandlerDecorator<TIn>> logger) : HandlerDecoratorBase<TIn>(decorated)
    where TIn : CommandBase
{
    private readonly DbContext _dbContext = dbContext;
    private readonly ITransactionParticipant[] _participants = participants.ToArray();
    private readonly ILogger _logger = logger;

    public override async Task<Result> Handle(TIn input)
    {
        var ignoreAttribute = Attribute.GetCustomAttribute(input.GetType(), typeof(IgnoreTransactionAttribute));
        if (ignoreAttribute == null)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                Result attempt;
                try
                {
                    attempt = await Decorated.Handle(input);

                    if (attempt.IsSuccess)
                    {
                        await transaction.CommitAsync();
                    }
                }
                catch
                {
                    await TransactionSignals.Abandon(transaction, _participants, _logger);
                    throw;
                }

                if (!attempt.IsSuccess)
                {
                    await TransactionSignals.Abandon(transaction, _participants, _logger);
                }

                return attempt;
            });

            if (result.IsSuccess)
            {
                await TransactionSignals.Notify(_participants, p => p.Committed(), nameof(ITransactionParticipant.Committed), _logger);
            }

            return result;
        }

        return await Decorated.Handle(input);
    }
}

internal static class TransactionSignals
{
    public static async Task Abandon(IDbContextTransaction transaction, ITransactionParticipant[] participants, ILogger logger)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Explicit rollback of the command transaction failed.");
        }

        await Notify(participants, p => p.Abandoned(), nameof(ITransactionParticipant.Abandoned), logger);
    }

    public static async Task Notify(ITransactionParticipant[] participants, Func<ITransactionParticipant, Task> signal, string signalName, ILogger logger)
    {
        foreach (var participant in participants)
        {
            try
            {
                await signal(participant);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction participant {Participant} failed in {Signal}.", participant.GetType().FullName, signalName);
            }
        }
    }
}
