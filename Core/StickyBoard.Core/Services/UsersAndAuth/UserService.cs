using System.Text.Json;
using StickyBoard.Core.Auth;
using StickyBoard.Core.Common.Exceptions;
using StickyBoard.Core.DTOs.UsersAndAuth;
using StickyBoard.Core.Models;
using StickyBoard.Core.Repositories.Base;
using StickyBoard.Core.Repositories.UsersAndAuth.Contracts;
using StickyBoard.Core.Services.UsersAndAuth.Contracts;

namespace StickyBoard.Core.Services.UsersAndAuth;

public sealed class UserService : IUserService
{
    private readonly IAuthUserRepository _authUsers;
    private readonly IPasswordHasher _hasher;
    private readonly IUserRepository _users;

    public UserService(IUserRepository users, IAuthUserRepository authUsers, IPasswordHasher hasher)
    {
        _users = users;
        _authUsers = authUsers;
        _hasher = hasher;
    }

    // ============================================================
    // GET SELF
    // ============================================================
    public async Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return null;

        var u = await _users.GetByIdAsync(userId, ct);
        if (u is null) return null;

        return new UserSelfDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUri,
            Prefs = u.Prefs is null
                ? new object()
                : JsonSerializer.Deserialize<object>(u.Prefs.RootElement.GetRawText()) ?? new object(),
            CreatedAt = u.CreatedAt,
            Groups = u.Groups ?? []
        };
    }

    // ============================================================
    // GET SINGLE USER (admin lookup)
    // ============================================================
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return null;

        var au = await _authUsers.GetByUserIdAsync(id, ct);

        return new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUri
        };
    }

    // ============================================================
    // SEARCH USERS (no paging, basic UX)
    // ============================================================
    public async Task<IEnumerable<UserDto>> SearchAsync(string query, CancellationToken ct)
    {
        var users = await _users.SearchByDisplayNameAsync(query, ct);

        var count = 0;
        foreach (var user in users) count++;
        var result = new List<UserDto>(count);
        foreach (var u in users)
            result.Add(new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUri
            });
        return result;
    }

    // ============================================================
    // SEARCH USERS (paged)
    // ============================================================
    public async Task<PagedResult<UserDto>> SearchPagedAsync(
        string query, int limit, int offset, CancellationToken ct)
    {
        var page = await _users.SearchByDisplayNamePagedAsync(query, limit, offset, ct);

        // Pre-size list if possible
        var list = new List<UserDto>();

        foreach (var u in page.Items)
            list.Add(new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUri
            });

        return PagedResult<UserDto>.Create(list, page.Total, limit, offset);
    }


    // ============================================================
    // UPDATE PROFILE
    // ============================================================
    public async Task<bool> UpdateProfileAsync(Guid userId, UserUpdateDto dto, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("Invalid user id.");

        var u = await _users.GetByIdAsync(userId, ct);
        if (u is null)
            throw new NotFoundException("User not found.");

        u.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? u.DisplayName : dto.DisplayName;
        u.AvatarUri = dto.AvatarUrl ?? u.AvatarUri;

        if (dto.Prefs is not null)
            u.Prefs = JsonSerializer.SerializeToDocument(dto.Prefs);

        return await _users.UpdateAsync(u, ct);
    }

    // ============================================================
    // CHANGE PASSWORD (self)
    // ============================================================
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new ValidationException("Invalid user id.");
        if (dto is null || string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            throw new ValidationException("Old and new password required.");

        var au = await _authUsers.GetByUserIdAsync(userId, ct);
        if (au is null || !_hasher.Verify(dto.OldPassword, au.PasswordHash))
            throw new AuthInvalidException("Invalid password.");

        au.PasswordHash = _hasher.Hash(dto.NewPassword);
        return await _authUsers.UpdateAsync(au, ct);
    }

    // ============================================================
    // ADMIN: ROLE UPDATE
    // ============================================================
    public async Task<bool> UpdateRoleAsync(Guid userId, UserRole role, CancellationToken ct)
    {
        var au = await _authUsers.GetByUserIdAsync(userId, ct)
                 ?? throw new NotFoundException("User not found.");

        au.Role = role;
        return await _authUsers.UpdateAsync(au, ct);
    }

    // ============================================================
    // DELETE USER (admin)
    // ============================================================
    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct)
    {
        await _authUsers.DeleteAsync(userId, ct);
        return await _users.DeleteAsync(userId, ct);
    }

    // ============================================================
    // GROUPS
    // ============================================================
    public Task<string[]> GetGroupsAsync(Guid userId, CancellationToken ct)
    {
        return _users.GetAllUserGroupsAsync(userId, ct);
    }

    public Task<bool> SetGroupsAsync(Guid userId, string[] groups, CancellationToken ct)
    {
        return _users.SetUserGroupsAsync(userId, groups, ct);
    }

    public Task<int> AddGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct)
    {
        return _users.AddGroupsAsync(userId, groups, ct);
    }

    public Task<int> RemoveGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct)
    {
        return _users.RemoveGroupsAsync(userId, groups, ct);
    }

    public Task<int> RemoveGroupAsync(Guid userId, string group, CancellationToken ct)
    {
        return _users.RemoveGroupAsync(userId, group, ct);
    }

    public Task<int> RenameGroupAsync(Guid userId, string oldName, string newName, CancellationToken ct)
    {
        return _users.RenameGroupAsync(userId, oldName, newName, ct);
    }

    public Task<string[]> SearchGroupsAsync(Guid userId, string partial, CancellationToken ct)
    {
        return _users.SearchGroupsAsync(userId, partial, ct);
    }
}