using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth.Contracts;

public interface IAuthUserRepository : IRepository<AuthUser>
{
    Task<AuthUser?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken ct);

    // Admin tools
    Task<PagedResult<AuthUser>> GetPagedAsync(int limit, int offset, CancellationToken ct);
}