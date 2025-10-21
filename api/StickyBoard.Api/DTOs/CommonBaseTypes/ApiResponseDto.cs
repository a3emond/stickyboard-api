namespace StickyBoard.Api.DTOs.CommonBaseTypes;

public record ApiResponseDto<T>(
    bool Success,
    T? Data,
    ErrorDto? Error = null
);