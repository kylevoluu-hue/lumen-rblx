namespace Lumen.Core.Common;

/// <summary>
/// Lightweight railway-oriented result type. Expected/recoverable failures are
/// modelled as values rather than exceptions so that security-sensitive code
/// paths (validation, extraction, parsing) never rely on exception control flow
/// and can never be silently swallowed.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>Human-readable, privacy-safe error message. Never contains secrets.</summary>
    public string? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

/// <summary>Result carrying a value on success.</summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error) => new(false, default, error);

    /// <summary>Returns the value on success or throws with the (safe) error message. Use only when success is an invariant.</summary>
    public T ValueOrThrow() =>
        IsSuccess ? Value! : throw new InvalidOperationException(Error ?? "Result was a failure.");
}
