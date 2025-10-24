using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUri { get; set; }
        public UserRole Role { get; set; } = UserRole.user;
    }

    public class UpdateUserDto
    {
        public string? DisplayName { get; set; }
        public string? AvatarUri { get; set; }
        public Dictionary<string, object>? Prefs { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}