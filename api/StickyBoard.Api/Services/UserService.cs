using System.Text.Json;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Repositories.UsersAndAuth;

namespace StickyBoard.Api.Services;

public sealed class UserService
{
    private readonly UserRepository _users;
    private readonly AuthUserRepository _authUsers;
    private readonly IPasswordHasher _hasher;

    public UserService(UserRepository users, AuthUserRepository authUsers, IPasswordHasher hasher)
    {
        _users = users;
        _authUsers = authUsers;
        _hasher = hasher;
    }

    // ----------------------------------------------------------------------
    // GET: one user (admin or internal use)
    // ----------------------------------------------------------------------
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return null;

        var au = await _authUsers.GetByUserIdAsync(u.Id, ct);
        return ToUserDto(u, au?.Role ?? UserRole.user);
    }

    // ----------------------------------------------------------------------
    // GET: current user (self)
    // ----------------------------------------------------------------------
    public async Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return null;

        var u = await _users.GetByIdAsync(userId, ct);
        return u is null ? null : ToUserSelfDto(u);
    }

    // ----------------------------------------------------------------------
    // SEARCH: by display name (for collaborators / admin)
    // ----------------------------------------------------------------------
    public async Task<IEnumerable<UserDto>> SearchAsync(string query, CancellationToken ct)
    {
        var users = await _users.SearchByDisplayNameAsync(query, ct);
        var list = new List<UserDto>(capacity: 32);

        foreach (var u in users)
        {
            var au = await _authUsers.GetByUserIdAsync(u.Id, ct);
            list.Add(ToUserDto(u, au?.Role ?? UserRole.user));
        }
        return list;
    }

    // ----------------------------------------------------------------------
    // UPDATE PROFILE (self)
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateProfileAsync(Guid userId, UserUpdateDto dto, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Invalid user id.");

        var u = await _users.GetByIdAsync(userId, ct);
        if (u is null)
            throw new NotFoundException("User not found.");

        // Basic validation: optional, but keep it tidy
        if (string.IsNullOrWhiteSpace(dto.DisplayName) && dto.AvatarUrl is null && dto.Prefs is null)
            throw new ValidationException("Nothing to update.");

        u.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? u.DisplayName : dto.DisplayName;
        u.AvatarUri   = dto.AvatarUrl ?? u.AvatarUri;

        if (dto.Prefs is not null)
        {
            // Convert arbitrary object to JsonDocument
            u.Prefs = JsonSerializer.SerializeToDocument(dto.Prefs);
        }

        return await _users.UpdateAsync(u, ct);
    }

    // ----------------------------------------------------------------------
    // CHANGE PASSWORD (self)
    // ----------------------------------------------------------------------
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Invalid user id.");
        if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            throw new ValidationException("Both old and new passwords are required.");

        var au = await _authUsers.GetByUserIdAsync(userId, ct);
        if (au is null || !_hasher.Verify(dto.OldPassword, au.PasswordHash))
            throw new AuthInvalidException("Invalid old password.");

        au.PasswordHash = _hasher.Hash(dto.NewPassword);
        return await _authUsers.UpdateAsync(au, ct);
    }

    // ----------------------------------------------------------------------
    // ADMIN: UPDATE ROLE
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateRoleAsync(Guid userId, UserRole newRole, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Invalid user id.");

        var au = await _authUsers.GetByUserIdAsync(userId, ct);
        if (au is null)
            throw new NotFoundException("User not found.");

        au.Role = newRole;
        return await _authUsers.UpdateAsync(au, ct);
    }

    // ----------------------------------------------------------------------
    // ADMIN: DELETE USER (soft-delete users, soft-delete auth, or hard as per repos)
    // ----------------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Invalid user id.");

        // Delete auth first, then user (repos define soft/hard behavior)
        await _authUsers.DeleteAsync(userId, ct);
        return await _users.DeleteAsync(userId, ct);
    }

    // ---------------------- Mapping helpers -------------------------------
    private static UserDto ToUserDto(Models.UsersAndAuth.User u, UserRole role) => new()
    {
        Id          = u.Id,
        Email       = u.Email,
        DisplayName = u.DisplayName,
        AvatarUrl   = u.AvatarUri,
        Role        = role
    };

    private static UserSelfDto ToUserSelfDto(Models.UsersAndAuth.User u) => new()
    {
        Id          = u.Id,
        Email       = u.Email,
        DisplayName = u.DisplayName,
        AvatarUrl   = u.AvatarUri,
        Prefs       = u.Prefs is null ? new() : JsonSerializer.Deserialize<object>(u.Prefs.RootElement.GetRawText()) ?? new(),
        CreatedAt   = u.CreatedAt
    };
}
