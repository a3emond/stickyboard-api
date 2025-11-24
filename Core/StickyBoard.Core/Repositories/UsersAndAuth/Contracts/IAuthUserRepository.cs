using StickyBoard.Core.Models.UsersAndAuth;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.UsersAndAuth.Contracts;

public interface IAuthUserRepository : IRepository<AuthUser>
{
    Task<AuthUser?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken ct);

    // Admin tools
    Task<PagedResult<AuthUser>> GetPagedAsync(int limit, int offset, CancellationToken ct);
}