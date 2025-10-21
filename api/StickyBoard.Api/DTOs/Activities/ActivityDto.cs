namespace StickyBoard.Api.DTOs.Activities;

public record ActivityDto(
    Guid Id,
    Guid BoardId,
    Guid? CardId,
    Guid? ActorId,
    string ActType,
    object Payload,
    DateTime CreatedAt
);