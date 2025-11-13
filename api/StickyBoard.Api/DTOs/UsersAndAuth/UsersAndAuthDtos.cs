using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.UsersAndAuth;

// ============================================================
// USERS - BASE PUBLIC DTO (used in lists, mentions, lookups)
// ============================================================
public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

// ============================================================
// USERS - SELF DTO (full context for own profile screen)
// ============================================================
public sealed class UserSelfDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public object Prefs { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string[] Groups { get; set; } = Array.Empty<string>();
}

// ============================================================
// USERS - ADMIN USER DETAIL (admin panel user view)
// ============================================================
public sealed class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.user;
    public DateTime CreatedAt { get; set; }
    public string[] Groups { get; set; } = Array.Empty<string>();
}

// ============================================================
// USER UPDATE (self)
// ============================================================
public sealed class UserUpdateDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public object? Prefs { get; set; }
}

// ============================================================
// USER PASSWORD CHANGE (self)
// ============================================================
public sealed class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ============================================================
// USER CREATE (admin)
// ============================================================
public sealed class UserCreateDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.user;
}

// ============================================================
// USER ROLE UPDATE (admin)
// ============================================================
public sealed class UpdateUserRoleDto
{
    public UserRole Role { get; set; }
}

// ============================================================
// GROUPS DTOs (internal / admin / self expansion later)
// ============================================================
public sealed class UserGroupsDto
{
    public string[] Groups { get; set; } = Array.Empty<string>();
}

public sealed class UpdateUserGroupsDto
{
    public string[] Groups { get; set; } = Array.Empty<string>();
}

public sealed class ModifyUserGroupDto
{
    public string Group { get; set; } = string.Empty;
}

// ============================================================
// AUTH: REGISTER / LOGIN / REFRESH
// ============================================================
public sealed class RegisterDto : IInviteAware
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? InviteToken { get; set; }
}


public sealed class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserSelfDto User { get; set; } = new();
}

public sealed class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RefreshTokenResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

