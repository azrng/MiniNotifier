namespace MiniNotifier.Models.DTOs;

public sealed record AppUpdateOperationResult<T>
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;

    public T? Data { get; init; }

    public static AppUpdateOperationResult<T> Success(T data, string message = "")
    {
        return new AppUpdateOperationResult<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static AppUpdateOperationResult<T> Failure(string message, string errorCode)
    {
        return new AppUpdateOperationResult<T>
        {
            IsSuccess = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
