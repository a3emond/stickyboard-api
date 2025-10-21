namespace StickyBoard.Api.DTOs.Sync;

public record SyncPullResponseDto(
    string Cursor,
    List<OperationDto> Operations,
    string? NextCursor
);