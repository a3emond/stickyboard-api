namespace StickyBoard.Api.DTOs.Auth;

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role
);