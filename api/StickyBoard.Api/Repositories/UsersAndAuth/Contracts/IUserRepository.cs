using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth.Contracts;

public interface IUserRepository : IRepository<User>, ISyncRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    Task<IEnumerable<User>> SearchByDisplayNameAsync(string partial, CancellationToken ct);
    Task<PagedResult<User>>  SearchByDisplayNamePagedAsync(string partial, int limit, int offset, CancellationToken ct);

    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<IEnumerable<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken ct);

    // Groups
    Task<string[]> GetAllUserGroupsAsync(Guid userId, CancellationToken ct);
    Task<bool>     SetUserGroupsAsync(Guid userId, string[] groups, CancellationToken ct);
    Task<int>      AddGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct);
    Task<int>      RemoveGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct);
    Task<int>      RenameGroupAsync(Guid userId, string oldName, string newName, CancellationToken ct);
    Task<int>      RemoveGroupAsync(Guid userId, string group, CancellationToken ct);
    Task<IEnumerable<User>> GetUsersByGroupAsync(string group, CancellationToken ct);
    Task<string[]> SearchGroupsAsync(Guid userId, string partial, CancellationToken ct);
}