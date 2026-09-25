using Microsoft.EntityFrameworkCore;

namespace ITB.Repository.EntityFrameworkCore;

/// <summary>
/// Duck-typed inspection of PostgreSQL errors surfaced through <see cref="DbUpdateException"/>.
/// Reads SqlState / ConstraintName via reflection so caller modules do not need a hard
/// reference to the Npgsql package.
/// </summary>
public static class PostgresErrorDetection
{
    // PostgreSQL SQLSTATE codes — see https://www.postgresql.org/docs/current/errcodes-appendix.html
    public const string UniqueViolationSqlState = "23505";
    public const string SerializationFailureSqlState = "40001";

    public static bool IsUniqueViolation(DbUpdateException ex, string constraintName)
        => GetSqlState(ex) == UniqueViolationSqlState
           && string.Equals(GetConstraintName(ex), constraintName, StringComparison.Ordinal);

    public static bool IsSerializationFailure(DbUpdateException ex)
        => GetSqlState(ex) == SerializationFailureSqlState;

    private static string? GetSqlState(DbUpdateException ex)
        => ReadStringProperty(ex.InnerException, "SqlState");

    private static string? GetConstraintName(DbUpdateException ex)
        => ReadStringProperty(ex.InnerException, "ConstraintName");

    private static string? ReadStringProperty(Exception? inner, string propertyName)
        => inner?.GetType().GetProperty(propertyName)?.GetValue(inner) as string;
}
