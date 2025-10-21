namespace StickyBoard.Api.DTOs.Sync;

public record SyncPushResponseDto(
    List<Guid> Accepted,
    List<object> Conflicts
);