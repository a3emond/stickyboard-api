namespace StickyBoard.Api.DTOs.Tabs;

public record TabCreateDto(
    string Title,
    string Scope,
    Guid? SectionId
);