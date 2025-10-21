namespace StickyBoard.Api.DTOs.Sync;

public record OperationDto(
    Guid Id,
    string DeviceId,
    Guid UserId,
    string Entity,
    Guid EntityId,
    string OpType,
    object Payload,
    int? VersionPrev,
    int? VersionNext,
    DateTime CreatedAt
);