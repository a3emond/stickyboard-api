using StickyBoard.Core.Models.UsersAndAuth;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.UsersAndAuth.Contracts;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<bool> RevokeAllAsync(Guid userId, CancellationToken ct);
    Task<int> CleanupRevokedAsync(CancellationToken ct);
}