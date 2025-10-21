namespace StickyBoard.Api.DTOs.Users;

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUri,
    object Prefs
);