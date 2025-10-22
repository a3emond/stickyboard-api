// DTOs/AuthDtos.cs

using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Auth
{
    public sealed class LoginRequestDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class RegisterRequestDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    public sealed class AuthResponseDto
    {
        public string Token { get; set; } = "";
        public UserDto User { get; set; } = default!;
    }

    public sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? AvatarUri { get; set; }
        public UserRole Role { get; set; } = UserRole.user;
    }
}