namespace StickyBoard.Api.DTOs.Clusters;

public record ClusterDto(
    Guid Id,
    Guid BoardId,
    string ClusterType,
    object? RuleDef,
    object VisualMeta,
    DateTime CreatedAt,
    DateTime UpdatedAt
);