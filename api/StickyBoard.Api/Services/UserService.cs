using StickyBoard.Api.DTOs.Users;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories;
using System.Text.Json;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Utils;

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
    // GET SINGLE USER
    // ----------------------------------------------------------------------
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return null;

        var auth = await _authUsers.GetByUserIdAsync(user.Id, ct);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUri = user.AvatarUri,
            Role = auth?.Role ?? UserRole.user
        };
    }

    // ----------------------------------------------------------------------
    // SEARCH USERS BY NAME OR EMAIL (for collaborators / admin)
    // ----------------------------------------------------------------------
    public async Task<IEnumerable<UserDto>> SearchAsync(string query, CancellationToken ct)
    {
        var users = await _users.SearchByDisplayNameAsync(query, ct);
        var result = new List<UserDto>();

        foreach (var u in users)
        {
            var auth = await _authUsers.GetByUserIdAsync(u.Id, ct);
            result.Add(new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUri = u.AvatarUri,
                Role = auth?.Role ?? UserRole.user
            });
        }

        return result;
    }

    // ----------------------------------------------------------------------
    // UPDATE PROFILE (self-edit)
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateUserDto dto, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return false;

        user.DisplayName = dto.DisplayName ?? user.DisplayName;
        user.AvatarUri = dto.AvatarUri ?? user.AvatarUri;
        user.Prefs = dto.Prefs is not null ? JsonDocument.Parse(dto.Prefs.ToJson()) : user.Prefs;

        return await _users.UpdateAsync(user, ct);
    }

    // ----------------------------------------------------------------------
    // CHANGE PASSWORD (self)
    // ----------------------------------------------------------------------
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        var auth = await _authUsers.GetByUserIdAsync(userId, ct);
        if (auth is null || !_hasher.Verify(dto.OldPassword, auth.PasswordHash))
            throw new UnauthorizedAccessException("Invalid old password.");

        auth.PasswordHash = _hasher.Hash(dto.NewPassword);
        return await _authUsers.UpdateAsync(auth, ct);
    }

    // ----------------------------------------------------------------------
    // ADMIN: UPDATE ROLE
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateRoleAsync(Guid userId, UserRole newRole, CancellationToken ct)
    {
        var auth = await _authUsers.GetByUserIdAsync(userId, ct);
        if (auth is null) return false;

        auth.Role = newRole;
        return await _authUsers.UpdateAsync(auth, ct);
    }

    // ----------------------------------------------------------------------
    // ADMIN: DELETE USER
    // ----------------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct)
    {
        await _authUsers.DeleteAsync(userId, ct);
        return await _users.DeleteAsync(userId, ct);
    }
}
