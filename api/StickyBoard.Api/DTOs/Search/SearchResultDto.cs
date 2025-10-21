namespace StickyBoard.Api.DTOs.Search;

public record SearchResultDto(
    string Type,
    Guid Id,
    string Title,
    string? Snippet,
    double Score,
    Guid? BoardId
);