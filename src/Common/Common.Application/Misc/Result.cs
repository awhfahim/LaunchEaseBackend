namespace Common.Application.Misc;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string error, ErrorType errorType)
    {
        IsSuccess = false;
        Error = error;
        ErrorType = errorType;
    }

    private static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(string error, ErrorType errorType) => new(error, errorType);
    public static implicit operator Result<T>(T value) => Success(value);
}
