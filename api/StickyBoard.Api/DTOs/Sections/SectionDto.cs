namespace StickyBoard.Api.DTOs.Sections;

public record SectionDto(
    Guid Id,
    Guid BoardId,
    string Title,
    int Position,
    object LayoutMeta,
    DateTime CreatedAt,
    DateTime UpdatedAt
);