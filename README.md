# ITB.CQRS

CQRS, Repository and Specification building blocks for .NET 10 applications on Entity Framework Core.

| Package | Namespace | What it contains |
|---|---|---|
| `ITB.CQRS` | `ITB.CQRS` | Commands, queries, handlers, `IHandlerDispatcher`, decorators, filtered/paged list queries |
| `ITB.Repository.Abstraction` | `ITB.Repository.Abstraction` | `IReadRepository<T>`, `IRepository<T>` |
| `ITB.Repository.EntityFrameworkCore` | `ITB.Repository.EntityFrameworkCore` | EF Core repositories and `IUnitOfWork` |
| `ITB.Specification` | `ITB.Specification` | Specification pattern with `&`, `|`, `!` composition and includes |
| `ITB.ResultModel` | `ITB.Shared.Result` | `Result`, `Result<T>` and the `Failure` hierarchy |
| `ITB.Shared.Domain` | `ITB.Domain.Entities`, `ITB.Domain.Interfaces` | `IEntity`, `Entity<TKey>`, `IQueryableFilter<T>` |

Full usage guide with copy-pasteable boilerplate: [HOWTOUSE.md](https://github.com/ids-group/ITB.CQRS/blob/master/HOWTOUSE.md)

## Getting started

```bash
dotnet add package ITB.CQRS
dotnet add package ITB.Repository.EntityFrameworkCore
```

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
// Repositories and the transaction decorator resolve the base DbContext type.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddRepositories();
builder.Services.AddQueryableFilters(typeof(Program).Assembly);
builder.Services.AddSpecificationFactory();
builder.Services.AddCQRS([typeof(Program).Assembly]);
```

`AddCQRS` scans the given assemblies for handlers, FluentValidation validators, `IPermissionValidator<T>`,
`IAccessFilter<T>` and Mapster configurations.

## Commands and queries

```csharp
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
        return client.Id;
    }
}

// Controller / endpoint
var result = await dispatcher.Handle<CreateClient, int>(command);
```

Every handler is wrapped by decorators, outermost first:

1. **ErrorHandlerDecorator** turns an unhandled exception into an `ExceptionFailure` (configurable through `CQRSOptions.ExceptionHandler`).
2. **PermissionValidationHandlerDecorator** runs every `IPermissionValidator<TIn>` and returns `ForbiddenFailure` on failure.
3. **ValidationHandlerDecorator** runs every FluentValidation `IValidator<TIn>` and returns `ValidationFailure`.
4. **TransactionHandlerDecorator** wraps commands (`CommandBase`) in a database transaction inside the EF execution strategy and commits only on success. Mark a command with `[IgnoreTransaction]` to opt out.

## Filtered lists

Derive the query from `GetFilteredListQuery<TOut>` and the handler from `GetFilteredListQueryHandler<TIn, TOut, TEntity>`.
Override `CheckAccess`, `BuildSpecification`, `BuildBaseQuery`, `Search`, `Sort` or `GetItems` as needed; the result
is projected to `TOut` with Mapster and returned as `PagedList<TOut>` with the total count.

Helpers:

- `FilterValidationExtensions.MustBeValidSortPath(...)` rejects unknown sort columns in a validator.
- `LikePatterns.Contains(value)` escapes `%`, `_` and `\` for `EF.Functions.Like(..., LikePatterns.EscapeChar)`.

## Migrating from 1.x

2.0 is a breaking release:

- Target framework is **net10.0**.
- **SimpleInjector was replaced by Microsoft.Extensions.DependencyInjection** (with Scrutor). `AddCQRS` no longer takes a `Container`.
- **AutoMapper was replaced by Mapster.** `GetFilteredListQueryHandler` no longer takes an `IMapper`.
- Handler contracts dropped the `Task<Result<...>>` wrapper from the generic argument: `ICommand<TOut>` / `IHandler<TIn, TOut>` now return `Task<Result<TOut>>`, and result-less commands use `ICommand` / `IHandler<TIn>`. `HandlerBase` was removed.
- `SortOrder` is now an enum; `Sorting.Path` no longer parses `.Asc` / `.Desc` suffixes.
- `PagedList<T>.Count` is `int` (always populated) instead of `int?`.
- `ApplyPageFilter` now defaults to 10 000 items when no page size is given, and clamps negative sizes and overflowing offsets.
- `AddRepositories()` returns `IServiceCollection`; `RepositoryBuilder` was removed and `AddQueryableFilters` is an extension on `IServiceCollection`.
- Namespaces: `ITB.ResultModel` → `ITB.Shared.Result`, `ITB.Shared.Domain` → `ITB.Domain.Entities` / `ITB.Domain.Interfaces`.
- The previously separate repositories (`ITB.Repository`, `ITB.Specification`, `ITB.Shared`) now live here.

## Releasing

Packages are published by pushing a version tag:

```bash
git tag v2.0.0 && git push origin v2.0.0
```

A tag with a prerelease suffix (`v2.0.0-beta.1`) publishes prerelease packages.
