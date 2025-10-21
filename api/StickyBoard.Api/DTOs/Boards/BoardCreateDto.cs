namespace StickyBoard.Api.DTOs.Boards;

public record BoardCreateDto(
    string Title,
    string Visibility,
    object? Theme
);