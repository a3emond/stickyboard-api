using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.Common;

// ==========================================================
// 0) API Envelopes
// ==========================================================
public sealed class ApiResponseDto<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static ApiResponseDto<T> Ok(T data, string? message = null)
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponseDto<T> Ok(string? message = null)
        => new() { Success = true, Message = message };

    // For list types
    public static ApiResponseDto<IEnumerable<TItem>> OkList<TItem>(IEnumerable<TItem> data, string? message = null)
        => new ApiResponseDto<IEnumerable<TItem>>
        {
            Success = true,
            Message = message,
            Data = data
        };

    public static ApiResponseDto<T> Fail(string message)
        => new() { Success = false, Message = message };
}


public sealed class ErrorDto
{
    public ErrorCode Code { get; init; } = ErrorCode.SERVER_ERROR; // AUTH_INVALID, FORBIDDEN, etc.
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
}