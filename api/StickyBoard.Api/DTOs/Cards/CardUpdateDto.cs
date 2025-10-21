namespace StickyBoard.Api.DTOs.Cards;

public record CardUpdateDto(
    string? Title,
    string? Status,
    object? Content,
    int? Priority,
    Guid? AssigneeId,
    DateTime? DueDate,
    DateTime? StartTime,
    DateTime? EndTime
);