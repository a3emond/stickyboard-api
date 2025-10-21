namespace StickyBoard.Api.DTOs.Boards;

public record BoardUpdateDto(
    string? Title,
    object? Theme,
    string? Visibility
);