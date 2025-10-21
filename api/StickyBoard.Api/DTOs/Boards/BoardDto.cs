namespace StickyBoard.Api.DTOs.Boards;

public record BoardDto(
    Guid Id,
    string Title,
    string Visibility,
    object Theme,
    object Rules,
    DateTime CreatedAt,
    DateTime UpdatedAt
);