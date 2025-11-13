using StickyBoard.Api.DTOs.UsersAndAuth;
using StickyBoard.Api.Models;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Services.UsersAndAuth.Contracts
{
    public interface IUserService
    {
        // ------------------------------------------------------------
        // SELF
        // ------------------------------------------------------------
        Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct);

        // ------------------------------------------------------------
        // ADMIN: GET SINGLE USER
        // ------------------------------------------------------------
        Task<UserDto?> GetAsync(Guid id, CancellationToken ct);

        // ------------------------------------------------------------
        // SEARCH
        // ------------------------------------------------------------
        Task<IEnumerable<UserDto>> SearchAsync(string query, CancellationToken ct);

        Task<PagedResult<UserDto>> SearchPagedAsync(
            string query,
            int limit,
            int offset,
            CancellationToken ct);

        // ------------------------------------------------------------
        // SELF: UPDATE PROFILE
        // ------------------------------------------------------------
        Task<bool> UpdateProfileAsync(Guid userId, UserUpdateDto dto, CancellationToken ct);

        // ------------------------------------------------------------
        // SELF: CHANGE PASSWORD
        // ------------------------------------------------------------
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct);

        // ------------------------------------------------------------
        // ADMIN: UPDATE ROLE
        // ------------------------------------------------------------
        Task<bool> UpdateRoleAsync(Guid userId, UserRole role, CancellationToken ct);

        // ------------------------------------------------------------
        // ADMIN: DELETE USER
        // ------------------------------------------------------------
        Task<bool> DeleteAsync(Guid userId, CancellationToken ct);

        // ------------------------------------------------------------
        // GROUPS
        // ------------------------------------------------------------
        Task<string[]> GetGroupsAsync(Guid userId, CancellationToken ct);

        Task<bool> SetGroupsAsync(Guid userId, string[] groups, CancellationToken ct);

        Task<int> AddGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct);

        Task<int> RemoveGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct);

        Task<int> RemoveGroupAsync(Guid userId, string group, CancellationToken ct);

        Task<int> RenameGroupAsync(Guid userId, string oldName, string newName, CancellationToken ct);

        Task<string[]> SearchGroupsAsync(Guid userId, string partial, CancellationToken ct);
    }
}
