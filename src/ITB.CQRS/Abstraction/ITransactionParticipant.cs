namespace ITB.CQRS.Abstraction;

/// <summary>
/// Gets told by <see cref="Decorators.TransactionHandlerDecorator{TIn}"/> how a command's transaction ended.
/// Register it in DI (scoped, so the handler and the decorator share one instance).
/// </summary>
public interface ITransactionParticipant
{
    /// <summary>The command's transaction committed. Fires once, after the last retry attempt.</summary>
    Task Committed();

    /// <summary>This attempt was rolled back. May fire once per attempt if the execution strategy retries.</summary>
    Task Abandoned();
}
