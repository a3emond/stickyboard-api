namespace StickyBoard.Api.DTOs.Rules;

public record RuleDto(
    Guid Id,
    Guid BoardId,
    object Definition,
    bool Enabled,
    DateTime CreatedAt,
    DateTime UpdatedAt
);