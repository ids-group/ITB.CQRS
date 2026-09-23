using ITB.CQRS.Abstraction;
using ITB.Shared.Result;
using Microsoft.EntityFrameworkCore;

namespace ITB.CQRS.Decorators;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreTransactionAttribute : Attribute
{
}

public class TransactionHandlerDecorator<TIn, TOut>(IHandler<TIn, TOut> decorated, DbContext dbContext) : HandlerDecoratorBase<TIn, TOut>(decorated)
    where TIn : CommandBase<TOut>
{
    private readonly DbContext _dbContext = dbContext;

    public override async Task<Result<TOut>> Handle(TIn input)
    {
        var ignoreAttribute = Attribute.GetCustomAttribute(input.GetType(), typeof(IgnoreTransactionAttribute));
        if (ignoreAttribute == null)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var result = await Decorated.Handle(input);

                if (result.IsSuccess)
                {
                    await transaction.CommitAsync();
                }

                return result;
            });
        }

        return await Decorated.Handle(input);
    }
}

public class TransactionHandlerDecorator<TIn>(IHandler<TIn> decorated, DbContext dbContext) : HandlerDecoratorBase<TIn>(decorated)
    where TIn : CommandBase
{
    private readonly DbContext _dbContext = dbContext;

    public override async Task<Result> Handle(TIn input)
    {
        var ignoreAttribute = Attribute.GetCustomAttribute(input.GetType(), typeof(IgnoreTransactionAttribute));
        if (ignoreAttribute == null)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var result = await Decorated.Handle(input);

                if (result.IsSuccess)
                {
                    await transaction.CommitAsync();
                }

                return result;
            });
        }

        return await Decorated.Handle(input);
    }
}
