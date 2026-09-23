namespace ITB.CQRS;

// Escapes the LIKE wildcards (%, _, \) in user input, so someone searching for "50%" matches a literal
// percent sign rather than "any sequence of characters".
public static class LikePatterns
{
    // Pass this as the third argument to EF.Functions.Like, or Postgres ignores the escapes added below.
    public const string EscapeChar = "\\";

    public static string Escape(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

    public static string Contains(string value) => "%" + Escape(value) + "%";
}
