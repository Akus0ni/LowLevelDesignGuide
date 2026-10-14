namespace Lld.Domain.Common;

/// <summary>
/// A failure with a stable, machine-readable <see cref="Code"/> (e.g. <c>access_request.not_pending</c>)
/// and a human-readable <see cref="Message"/>. The API maps codes to HTTP status codes in one place.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static Error Validation(string code, string message) => new($"validation.{code}", message);
}

/// <summary>Outcome of an operation that returns no value. Failures are values, not exceptions.</summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that returns a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error)
        : base(error) => _value = value;

    /// <summary>The value; only valid when <see cref="Result.IsSuccess"/> is true.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"No value for a failed result ({Error!.Code}).");

    internal static Result<T> Success(T value) => new(value, null);

    internal static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
