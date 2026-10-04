using System.Diagnostics.CodeAnalysis;

namespace MiiAnimator.MiiData;

// Minimal stand-in for WheelWizard's OperationResult so the copied Mii code compiles unchanged.

public class OperationError
{
    public required string Message { get; init; }
    public Exception? Exception { get; init; }

    public static OperationError Fail(string error) => error;

    public static OperationError Fail(Exception error, object? translation = null, object[]? titleReplacements = null, object[]? extra = null) =>
        error;

    public static implicit operator OperationError(string errorMessage) => new() { Message = errorMessage };

    public static implicit operator OperationError(Exception exception) => new() { Message = exception.Message, Exception = exception };

    public override string ToString() => Message;
}

public class OperationResult
{
    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public virtual bool IsFailure => !IsSuccess;

    public OperationError? Error { get; }

    public OperationResult() { }

    public OperationResult(OperationError error) => Error = error;

    public static OperationResult Ok() => new();

    public static OperationResult<T> Ok<T>(T value) => new(value);

    public static OperationResult<T> TryCatch<T>(Func<T> func)
    {
        try
        {
            return Ok(func());
        }
        catch (Exception exception)
        {
            return new OperationResult<T>((OperationError)exception);
        }
    }

    public static implicit operator OperationResult(OperationError error) => new(error);

    public static implicit operator OperationResult(Exception exception) => new(exception);
}

public class OperationResult<T> : OperationResult
{
    private readonly T _value;

    public T Value => IsSuccess ? _value : throw new InvalidOperationException($"The operation was not successful: {Error}");

    public OperationResult(T value) => _value = value;

    public OperationResult(OperationError error)
        : base(error) => _value = default!;

    public static implicit operator OperationResult<T>(T value) => new(value);

    public static implicit operator OperationResult<T>(OperationError error) => new(error);

    public static implicit operator OperationResult<T>(Exception exception) => new((OperationError)exception);
}
