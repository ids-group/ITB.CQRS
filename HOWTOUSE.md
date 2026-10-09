# How to use ITB.CQRS

Copy-pasteable boilerplate for the packages described in [README.md](https://github.com/ids-group/ITB.CQRS/blob/master/README.md):
DI wiring, one running example domain, a command with a result, a result-less command, a query, a
filtered/paged list query, and the three decorator hooks.

Every `csharp` block below opens with the `using` directives its body needs, and every block builds on the
same two types (`Client`, `AppDbContext`), so you can paste them one after another into a fresh project.

## Install

```bash
dotnet add package ITB.CQRS
dotnet add package ITB.Repository.EntityFrameworkCore
```

`ITB.CQRS` brings in `ITB.Repository.Abstraction`, `ITB.Specification`, `ITB.ResultModel` (namespace
`ITB.Shared.Result`) and `ITB.Shared.Domain` (namespaces `ITB.Domain.Entities` and `ITB.Domain.Interfaces`)
as transitive dependencies.

## The running example

One entity and one `DbContext`. Everything further down uses only these two types plus types from the
packages.

```csharp
using ITB.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class Client : Entity<int>
{
    // EF Core materializes through this one; Id is set by the database.
    protected Client()
    {
    }

    public Client(string name)
    {
        Name = name;
        IsActive = true;
    }

    public string Name { get; set; }
    public bool IsActive { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients { get; set; }
}
```

`Entity<TKey>` gives you `Id` and `GetKeys()`; the repositories only require `TEntity : class, IEntity`.

## Wiring

The whole registration, in order, as `Program.cs` top-level statements:

```csharp
using ITB.CQRS;
using ITB.Repository.EntityFrameworkCore;
using ITB.Shared.Result;
using ITB.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Your provider, for example:
    // options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

// Forward the derived context to the base type. See the note below - this line is easy to forget and
// the failure only shows up at runtime.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddRepositories();
builder.Services.AddQueryableFilters(typeof(Program).Assembly);
builder.Services.AddSpecificationFactory();

builder.Services.AddCQRS([typeof(Program).Assembly], options =>
{
    options.ExceptionHandler = (exception, logger) =>
    {
        logger.LogError(exception, "Unhandled exception in a handler.");
        return new ExceptionFailure(exception);
    };
});

var app = builder.Build();
app.Run();
```

**Why `AddScoped<DbContext>`.** `Repository<T>`, `ReadRepository<T>`, `UnitOfWork` and
`TransactionHandlerDecorator` all inject the base `Microsoft.EntityFrameworkCore.DbContext`, not your
derived `AppDbContext`. `AddDbContext<AppDbContext>` registers only `AppDbContext`, so without the
forwarding registration every handler fails to resolve.

**What `AddCQRS` does.** It scans the given assemblies for handlers (`IHandler<,>` / `IHandler<>`),
FluentValidation `IValidator<>`, `IPermissionValidator<>`, `IAccessFilter<>` and Mapster configurations,
registers `IHandlerDispatcher`, and wraps every handler in the decorators listed under
[Decorator hooks](#decorator-hooks). The optional `Action<CQRSOptions>` overload sets
`CQRSOptions.ExceptionHandler` (`Func<Exception, ILogger, Failure>`), which `ErrorHandlerDecorator` calls
for any exception a handler lets through. The default handler logs the exception and returns
`new ExceptionFailure(exception)`.

`AddQueryableFilters` registers your `IQueryableFilter<T>` / `IAccessFilter<T>` implementations; every
repository query applies them automatically.

## A command with a result

```csharp
using ITB.CQRS;
using ITB.Repository.Abstraction;
using ITB.Repository.EntityFrameworkCore;
using ITB.Shared.Result;

public class CreateClient : CommandBase<int>
{
    public string Name { get; set; }
}

public class CreateClientHandler(IRepository<Client> clients, IUnitOfWork unitOfWork)
    : CommandHandlerBase<CreateClient, int>
{
    public override async Task<Result<int>> Handle(CreateClient input)
    {
        var client = await clients.Add(new Client(input.Name));
        await unitOfWork.SaveChanges();

        // Implicit conversion TOut -> Result<TOut>; no need to write new Result<int>(client.Id).
        return client.Id;
    }
}
```

## A result-less command

```csharp
using ITB.CQRS;
using ITB.Repository.Abstraction;
using ITB.Repository.EntityFrameworkCore;
using ITB.Shared.Result;

public class DeleteClient : CommandBase
{
    public int Id { get; set; }
}

public class DeleteClientHandler(IRepository<Client> clients, IUnitOfWork unitOfWork)
    : CommandHandlerBase<DeleteClient>
{
    public override async Task<Result> Handle(DeleteClient input)
    {
        var client = await clients.FirstOrDefault(c => c.Id == input.Id);
        if (client == null)
        {
            // Result.NotFound returns a Failure; the implicit Failure -> Result conversion does the rest.
            return Result.NotFound($"Client {input.Id} not found.");
        }

        await clients.Delete(client);
        await unitOfWork.SaveChanges();

        return Result.Success();
    }
}
```

## Derive from `CommandBase`, not just `ICommand`

A command must derive from `CommandBase<TOut>` or `CommandBase`. Implementing `ICommand<TOut>` / `ICommand`
directly compiles, but two constraints in the library are written against the concrete classes:

- `IHandlerDispatcher.Handle<TIn>(TIn input) where TIn : CommandBase` - a type that only implements
  `ICommand` cannot use the result-less dispatch overload.
- `TransactionHandlerDecorator<TIn, TOut> where TIn : CommandBase<TOut>` and
  `TransactionHandlerDecorator<TIn> where TIn : CommandBase` - a type that only implements the interface is
  never wrapped in a transaction, so a handler that writes several rows can commit half of them.

Queries have no such requirement: `QueryBase<TOut>` is a convenience, and the query path is not
transactional by design.

## Dispatching and reading the `Result`

```csharp
using ITB.CQRS;
using ITB.Shared.Result;

public class ClientsService(IHandlerDispatcher dispatcher)
{
    public async Task<int> Create(string name)
    {
        // Task<Result<TOut>> Handle<TIn, TOut>(TIn input) where TIn : IRequest<TOut>
        var result = await dispatcher.Handle<CreateClient, int>(new CreateClient { Name = name });

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Failure.Message);
        }

        return result.Data;
    }

    public async Task<string> Delete(int id)
    {
        // Task<Result> Handle<TIn>(TIn input) where TIn : CommandBase - no type arguments needed.
        var result = await dispatcher.Handle(new DeleteClient { Id = id });

        return result.Failure switch
        {
            null => "204 No Content",
            NotFoundFailure failure => $"404 {failure.Message}",
            ValidationFailure failure => $"422 {failure.Message}",
            ForbiddenFailure failure => $"403 {failure.Message}",
            _ => "500 Internal Server Error"
        };
    }
}
```

A handler never throws at the caller: the decorators translate everything into a `Result`. Inspect
`IsSuccess`, read the payload from `Data` and branch on the `Failure` subtype:

| `Failure` subtype | Suggested status | Produced by |
|---|---|---|
| `NotFoundFailure` | 404 | `Result.NotFound(message)` |
| `UnauthorizedFailure` | 401 | `Result.Unauthorized(message)` |
| `ForbiddenFailure` | 403 | `Result.Forbidden(message)`, `PermissionValidationHandlerDecorator` |
| `ConflictFailure` / `CodedConflictFailure` | 409 | `Result.Conflict(message)` / `Result.Conflict(code, message)` |
| `LockedFailure` | 429 | `Result.Locked(message)` |
| `ValidationFailure` | 422 | `Result.ValidationError(...)`, `ValidationHandlerDecorator` |
| `ExceptionFailure` | 500 | `ErrorHandlerDecorator` through `CQRSOptions.ExceptionHandler` |
| `Failure` | 400 | `Result.Fail(message)` |

## A query

```csharp
using ITB.CQRS;
using ITB.Repository.Abstraction;
using ITB.Shared.Result;

public class ClientDetails
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

public class GetClient : QueryBase<ClientDetails>
{
    public int Id { get; set; }
}

public class GetClientHandler(IReadRepository<Client> clients)
    : QueryHandlerBase<GetClient, ClientDetails>
{
    public override async Task<Result<ClientDetails>> Handle(GetClient input)
    {
        var client = await clients.FirstOrDefault(c => c.Id == input.Id);
        if (client == null)
        {
            return Result.NotFound($"Client {input.Id} not found.");
        }

        return new ClientDetails
        {
            Id = client.Id,
            Name = client.Name,
            IsActive = client.IsActive
        };
    }
}
```

Queries take `IReadRepository<T>`: it has no `Add` / `Update` / `Delete`, which keeps the read path honest.

## A filtered and paged list

`GetFilteredListQuery<TOut>` derives from `FilterBase`, so the query already carries `SearchText`, `Paging`
and `Sorting`; add your own filter fields on top. The handler returns
`Result<PagedList<ClientListItem>>` - the page plus the total count of matching rows.

```csharp
using FluentValidation;
using ITB.CQRS;
using ITB.CQRS.Models;
using ITB.Repository.Abstraction;
using ITB.Specification;
using Microsoft.EntityFrameworkCore;

public class ClientListItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

public class GetClients : GetFilteredListQuery<ClientListItem>
{
    public bool OnlyActive { get; set; }
}

public class GetClientsHandler(IReadRepository<Client> repository)
    : GetFilteredListQueryHandler<GetClients, ClientListItem, Client>(repository)
{
    // Narrows the base query. Runs after CheckAccess and before BuildBaseQuery.
    protected override Task BuildSpecification(GetClients input)
    {
        if (input.OnlyActive)
        {
            Specification = Specification.And(c => c.IsActive);
        }

        return Task.CompletedTask;
    }

    // searchText is input.SearchText; the total count is taken after Search and before Sort.
    protected override IQueryable<Client> Search(IQueryable<Client> query, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        return query.Where(c =>
            EF.Functions.Like(c.Name, LikePatterns.Contains(searchText), LikePatterns.EscapeChar));
    }

    // sorting is null when the caller sent none; always end with a unique column so paging is stable.
    protected override IQueryable<Client> Sort(IQueryable<Client> query, Sorting sorting)
    {
        var ascending = sorting == null || sorting.Order == SortOrder.Asc;

        return sorting?.Path switch
        {
            "name" => ascending
                ? query.OrderBy(c => c.Name).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.Name).ThenBy(c => c.Id),
            _ => ascending
                ? query.OrderBy(c => c.Id)
                : query.OrderByDescending(c => c.Id)
        };
    }
}

public class GetClientsValidator : AbstractValidator<GetClients>
{
    public GetClientsValidator()
    {
        // Rejects an unknown sort path with a 422 listing the valid columns; sending no sort stays valid.
        RuleFor(q => q.Sorting.Path).MustBeValidSortPath("name", "id");
    }
}
```

`LikePatterns.Contains` escapes `%`, `_` and `\` in the user's input, and `LikePatterns.EscapeChar` is the
third argument `EF.Functions.Like` needs for those escapes to reach the database.

`GetItems` projects the paged query with Mapster (`ProjectToType<TOut>()`), so every member of `TOut` must
be mappable from the entity - either by matching name or through a Mapster configuration picked up by
`AddCQRS`. The remaining hooks you can override are `CheckAccess` (reject the request before any query
runs, returning a failed `Result`), `BuildBaseQuery` (start from something other than
`Repository.Query(Specification)`) and `GetItems` itself (project by hand instead of with Mapster).

## Decorator hooks

Every handler is wrapped, outermost first:

1. **`ErrorHandlerDecorator`** - catches anything the handler throws and turns it into a `Failure` through
   `CQRSOptions.ExceptionHandler`.
2. **`PermissionValidationHandlerDecorator`** - runs every registered `IPermissionValidator<TIn>` and
   returns `ForbiddenFailure` on the first invalid result.
3. **`ValidationHandlerDecorator`** - runs every registered FluentValidation `IValidator<TIn>` and returns
   `ValidationFailure` with one `ValidationError` per broken rule.
4. **`TransactionHandlerDecorator`** - opens a transaction inside the EF execution strategy for commands,
   commits only when the `Result` is successful and rolls back otherwise, then tells every registered
   `ITransactionParticipant` how it ended (see [After commit](#after-commit)).

That order is the reverse of the registration order in `CQRSServiceCollectionExtensions`
(`TryDecorate(Transaction)`, `Decorate(Validation)`, `Decorate(Permission)`, `Decorate(Error)`): the last
decoration ends up outermost. So permissions are checked before the payload is validated, and the
transaction only ever wraps the handler itself.

### Input validation

```csharp
using FluentValidation;

public class CreateClientValidator : AbstractValidator<CreateClient>
{
    public CreateClientValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
```

### Permissions

```csharp
using ITB.CQRS.Models;

// Your own abstraction over the signed-in user; register it in DI as usual.
public interface ICurrentUser
{
    bool IsInRole(string role);
}

public class CreateClientPermissionValidator(ICurrentUser currentUser)
    : IPermissionValidator<CreateClient>
{
    public Task<PermissionValidationResult> Validate(CreateClient input, CancellationToken cancellation = default)
    {
        if (!currentUser.IsInRole("ClientManager"))
        {
            // Any message makes IsValid false; the decorator turns it into a ForbiddenFailure.
            return Task.FromResult(new PermissionValidationResult("Only client managers may create clients."));
        }

        // Parameterless: no errors, permission granted.
        return Task.FromResult(new PermissionValidationResult());
    }
}
```

### Opting out of the transaction

```csharp
using ITB.CQRS;
using ITB.CQRS.Decorators;
using ITB.Repository.Abstraction;
using ITB.Repository.EntityFrameworkCore;
using ITB.Shared.Result;

// Long-running work that must not hold a transaction open, or a command that manages its own.
[IgnoreTransaction]
public class ImportClients : CommandBase
{
    public List<string> Names { get; set; } = new();
}

public class ImportClientsHandler(IRepository<Client> clients, IUnitOfWork unitOfWork)
    : CommandHandlerBase<ImportClients>
{
    public override async Task<Result> Handle(ImportClients input)
    {
        foreach (var name in input.Names)
        {
            await clients.Add(new Client(name));
            await unitOfWork.SaveChanges();
        }

        return Result.Success();
    }
}
```

`IgnoreTransactionAttribute` lives in `ITB.CQRS.Decorators`, not `ITB.CQRS`. Without it the command runs
inside a single transaction that commits only when the whole `Result` is successful, which is what you want
for almost everything else.

### After commit

Some side effects must not happen until the data is really in the database: an email about a client
that a later rollback removes is worse than no email. Implement `ITransactionParticipant` to hear how the
command's transaction ended, buffer the work in the handler, and flush it in `Committed`.

```csharp
using ITB.CQRS;
using ITB.CQRS.Abstraction;
using ITB.Repository.Abstraction;
using ITB.Repository.EntityFrameworkCore;
using ITB.Shared.Result;

// Your own abstraction over the mail service; register it in DI as usual.
public interface IEmailSender
{
    Task Send(string to, string subject);
}

// Collects emails during the command and sends them only once the transaction has committed.
public class ClientEmails(IEmailSender sender) : ITransactionParticipant
{
    private readonly List<string> _subjects = new();

    public void Enqueue(string subject) => _subjects.Add(subject);

    public async Task Committed()
    {
        foreach (var subject in _subjects)
        {
            await sender.Send("clients@example.com", subject);
        }

        _subjects.Clear();
    }

    // Called once per rolled-back attempt, so a retry starts from an empty buffer.
    public Task Abandoned()
    {
        _subjects.Clear();
        return Task.CompletedTask;
    }
}

public class RegisterClient : CommandBase<int>
{
    public string Name { get; set; }
}

public class RegisterClientHandler(IRepository<Client> clients, IUnitOfWork unitOfWork, ClientEmails emails)
    : CommandHandlerBase<RegisterClient, int>
{
    public override async Task<Result<int>> Handle(RegisterClient input)
    {
        var client = await clients.Add(new Client(input.Name));
        await unitOfWork.SaveChanges();

        emails.Enqueue($"Client {client.Name} registered");

        return client.Id;
    }
}
```

Register the participant as scoped, once under its own type for the handler and once forwarded to
`ITransactionParticipant` for the decorator, so both get the same instance:

```csharp
using ITB.CQRS.Abstraction;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddScoped<ClientEmails>();
builder.Services.AddScoped<ITransactionParticipant>(sp => sp.GetRequiredService<ClientEmails>());
```

What the decorator guarantees:

- **`Committed()`** fires once, after the execution strategy has returned, so it never fires for an
  attempt that is later retried.
- **`Abandoned()`** fires after the transaction is rolled back, for a failed `Result` and for an exception
  alike (commit failures included). With a retrying execution strategy it can fire once per attempt. The
  exception is rethrown afterwards, so `ErrorHandlerDecorator` still turns it into a `Failure`.
- **A participant that throws is logged and swallowed.** In `Committed()` the data is already in the
  database, so the command still returns success; in `Abandoned()` the original failure or exception is
  kept. The remaining participants are still called.
- **`[IgnoreTransaction]` commands send no signals.** There is no transaction to report on.
- Every registered participant hears about every transactional command in the scope, not only the ones
  that used it, so keep `Committed()` and `Abandoned()` cheap when there is nothing buffered.
- The rollback is explicit (`RollbackAsync()`), so EF's own rollback notifications - for example a
  `DbTransactionInterceptor.TransactionRolledBack` - fire as well.
