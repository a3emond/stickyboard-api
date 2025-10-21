namespace StickyBoard.Api.DTOs.Cards;

public record CardCreateDto(
    string Type,
    string? Title,
    object? Content,
    Guid? SectionId,
    Guid? TabId
);