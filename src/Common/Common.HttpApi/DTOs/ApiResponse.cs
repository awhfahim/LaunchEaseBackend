namespace Common.HttpApi.DTOs;

public class ApiResponse<T>
{
    public required bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public ICollection<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResult(string message, ICollection<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}