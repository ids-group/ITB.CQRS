using System.Data.Common;
using ITB.CQRS.Abstraction;
using ITB.CQRS.Decorators;
using ITB.Shared.Result;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ITB.CQRS.UnitTests.Tests;

public sealed class TransactionHandlerDecoratorTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly RollbackCounter _rollbacks = new();
    private readonly TestDbContext _context;
    private readonly RecordingParticipant _participant = new();

    public TransactionHandlerDecoratorTests()
    {
        _connection.Open();
        _context = CreateContext();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task A_successful_command_signals_committed_once()
    {
        var decorator = Decorate(new AddRowHandler(_context, _ => Result.Success()), _participant);

        var result = await decorator.Handle(new AddRow());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _participant.CommittedCount);
        Assert.Equal(0, _participant.AbandonedCount);
        Assert.Equal(1, await CountRows());
    }

    [Fact]
    public async Task A_failed_result_signals_abandoned_and_leaves_the_row_absent()
    {
        var decorator = Decorate(new AddRowHandler(_context, _ => Result.Conflict("taken")), _participant);

        var result = await decorator.Handle(new AddRow());

        Assert.IsType<ConflictFailure>(result.Failure);
        Assert.Equal(0, _participant.CommittedCount);
        Assert.Equal(1, _participant.AbandonedCount);
        Assert.Equal(0, await CountRows());
        // The rollback is explicit now, so EF's own rollback notification fires too.
        Assert.Equal(1, _rollbacks.Count);
    }

    [Fact]
    public async Task An_exception_signals_abandoned_and_still_comes_back_as_a_failure()
    {
        var transaction = Decorate(new AddRowHandler(_context, _ => throw new InvalidOperationException("boom")), _participant);
        var decorator = new ErrorHandlerDecorator<AddRow, int>(
            transaction,
            Options.Create(new CQRSOptions()),
            NullLogger<ErrorHandlerDecorator<AddRow, int>>.Instance);

        var result = await decorator.Handle(new AddRow());

        var failure = Assert.IsType<ExceptionFailure>(result.Failure);
        Assert.Equal("boom", failure.Exception.Message);
        Assert.Equal(0, _participant.CommittedCount);
        Assert.Equal(1, _participant.AbandonedCount);
        Assert.Equal(0, await CountRows());
    }

    [Fact]
    public async Task An_ignored_transaction_signals_nothing()
    {
        var decorator = new TransactionHandlerDecorator<AddRowWithoutTransaction, int>(
            new AddRowHandler(_context, _ => Result.Success()),
            _context,
            [_participant],
            NullLogger<TransactionHandlerDecorator<AddRowWithoutTransaction, int>>.Instance);

        var result = await decorator.Handle(new AddRowWithoutTransaction());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _participant.CommittedCount);
        Assert.Equal(0, _participant.AbandonedCount);
        Assert.Equal(1, await CountRows());
    }

    [Fact]
    public async Task A_throwing_participant_does_not_fail_a_committed_command()
    {
        var logger = new ListLogger<TransactionHandlerDecorator<AddRow, int>>();
        var decorator = new TransactionHandlerDecorator<AddRow, int>(
            new AddRowHandler(_context, _ => Result.Success()),
            _context,
            [new ThrowingParticipant(), _participant],
            logger);

        var result = await decorator.Handle(new AddRow());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await CountRows());
        // The participants after the throwing one are still told.
        Assert.Equal(1, _participant.CommittedCount);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public async Task A_result_less_command_signals_committed_on_success()
    {
        var decorator = DecorateResultLess(new AddRowCommandHandler(_context, Result.Success()));

        var result = await decorator.Handle(new AddRowCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _participant.CommittedCount);
        Assert.Equal(0, _participant.AbandonedCount);
        Assert.Equal(1, await CountRows());
    }

    [Fact]
    public async Task A_result_less_command_signals_abandoned_on_a_failed_result()
    {
        var decorator = DecorateResultLess(new AddRowCommandHandler(_context, Result.NotFound("missing")));

        var result = await decorator.Handle(new AddRowCommand());

        Assert.IsType<NotFoundFailure>(result.Failure);
        Assert.Equal(0, _participant.CommittedCount);
        Assert.Equal(1, _participant.AbandonedCount);
        Assert.Equal(0, await CountRows());
    }

    private TransactionHandlerDecorator<AddRow, int> Decorate(IHandler<AddRow, int> handler, params ITransactionParticipant[] participants) =>
        new(handler, _context, participants, NullLogger<TransactionHandlerDecorator<AddRow, int>>.Instance);

    private TransactionHandlerDecorator<AddRowCommand> DecorateResultLess(IHandler<AddRowCommand> handler) =>
        new(handler, _context, [_participant], NullLogger<TransactionHandlerDecorator<AddRowCommand>>.Instance);

    // A fresh context, so the count comes from the database and not from the change tracker.
    private async Task<int> CountRows()
    {
        await using var context = CreateContext();
        return await context.Rows.CountAsync();
    }

    private TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_rollbacks)
            .Options);

    public sealed class Row
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows { get; set; }
    }

    public sealed class AddRow : CommandBase<int>;

    [IgnoreTransaction]
    public sealed class AddRowWithoutTransaction : CommandBase<int>;

    public sealed class AddRowCommand : CommandBase;

    // Saves a row, then returns whatever the outcome says - a failure, or a success carrying the new id.
    private sealed class AddRowHandler(TestDbContext context, Func<int, Result> outcome)
        : IHandler<AddRow, int>, IHandler<AddRowWithoutTransaction, int>
    {
        public Task<Result<int>> Handle(AddRow input) => AddAndReturn();

        public Task<Result<int>> Handle(AddRowWithoutTransaction input) => AddAndReturn();

        private async Task<Result<int>> AddAndReturn()
        {
            var row = new Row { Name = "row" };
            context.Rows.Add(row);
            await context.SaveChangesAsync();

            var result = outcome(row.Id);
            return result.IsSuccess ? row.Id : result.Failure;
        }
    }

    private sealed class AddRowCommandHandler(TestDbContext context, Result outcome) : IHandler<AddRowCommand>
    {
        public async Task<Result> Handle(AddRowCommand input)
        {
            context.Rows.Add(new Row { Name = "row" });
            await context.SaveChangesAsync();
            return outcome;
        }
    }

    private sealed class RecordingParticipant : ITransactionParticipant
    {
        public int CommittedCount { get; private set; }
        public int AbandonedCount { get; private set; }

        public Task Committed()
        {
            CommittedCount++;
            return Task.CompletedTask;
        }

        public Task Abandoned()
        {
            AbandonedCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingParticipant : ITransactionParticipant
    {
        public Task Committed() => throw new InvalidOperationException("participant failed");

        public Task Abandoned() => throw new InvalidOperationException("participant failed");
    }

    private sealed class RollbackCounter : DbTransactionInterceptor
    {
        public int Count { get; private set; }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, exception));
    }
}
