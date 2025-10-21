namespace StickyBoard.Api.DTOs.Tabs;

public record TabDto(
    Guid Id,
    string Scope,
    Guid BoardId,
    Guid? SectionId,
    string Title,
    string TabType,
    object LayoutConfig,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt
);