namespace ITB.Shared.Result;

public class Failure(string message)
{
    public string Message { get; set; } = message;
}

public class NotFoundFailure(string message) : Failure(message)
{
}

public class UnauthorizedFailure(string message) : Failure(message)
{
}

public class ForbiddenFailure(string message) : Failure(message)
{
}

// HTTP 409: the operation cannot proceed given the resource's current state (duplicate, expired,
// already-used token, and so on).
public class ConflictFailure(string message) : Failure(message)
{
}

// 409 with a stable machine-readable code the client can use to drive UI behaviour.
public class CodedConflictFailure(string code, string message) : ConflictFailure(message)
{
    public string Code { get; } = code;
}

// HTTP 429, typically an account temporarily locked after repeated failed sign-in attempts.
public class LockedFailure(string message) : Failure(message)
{
}

public class ExceptionFailure : Failure
{
    // Server-side logging only. Never serialized to the client.
    [System.Text.Json.Serialization.JsonIgnore]
    public Exception? Exception { get; }

    public ExceptionFailure(Exception exception)
        : base("An internal error occurred.")
    {
        Exception = exception;
    }

    public ExceptionFailure(string message)
        : base(message)
    {
    }
}

public class ValidationFailure : Failure
{
    public ValidationError[] ValidationErrors { get; set; }

    public ValidationFailure(ValidationError[] validationErrors, string message = "Validation Error")
        : base(message)
    {
        if (validationErrors == null || validationErrors.Length == 0)
        {
            throw new ArgumentException("Validation errors must not be null or empty.", nameof(validationErrors));
        }

        ValidationErrors = validationErrors;
    }

    public ValidationFailure(ValidationError validationError, string message = "Validation Error")
        : this(new[] { validationError }, message)
    {
    }
}
