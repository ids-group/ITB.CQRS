using ITB.CQRS.Abstraction;

namespace ITB.CQRS;

public class CommandBase<TOut> : ICommand<TOut>
{
}

public class CommandBase : ICommand
{
}
