namespace StickyBoard.Api.DTOs.Links;

public record LinkDto(
    Guid Id,
    Guid FromCard,
    Guid ToCard,
    string RelType,
    Guid? CreatedBy,
    DateTime CreatedAt
);