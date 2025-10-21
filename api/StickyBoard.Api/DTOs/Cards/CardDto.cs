namespace StickyBoard.Api.DTOs.Cards;

public record CardDto(
    Guid Id,
    Guid BoardId,
    Guid? SectionId,
    Guid? TabId,
    string Type,
    string? Title,
    object Content,
    DateTime? DueDate,
    DateTime? StartTime,
    DateTime? EndTime,
    int? Priority,
    string Status,
    Guid? CreatedBy,
    Guid? AssigneeId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version
);