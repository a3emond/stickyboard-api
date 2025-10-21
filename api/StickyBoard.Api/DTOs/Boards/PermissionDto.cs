namespace StickyBoard.Api.DTOs.Boards;

public record PermissionDto(
    Guid UserId,
    Guid BoardId,
    string Role,
    DateTime GrantedAt
);