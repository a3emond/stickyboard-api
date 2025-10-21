namespace StickyBoard.Api.DTOs.Search;

public record SearchRequestDto(
    string Query,
    object? Filters
);