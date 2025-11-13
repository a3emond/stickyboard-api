using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth.Contracts;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<bool> RevokeAllAsync(Guid userId, CancellationToken ct);
    Task<int> CleanupRevokedAsync(CancellationToken ct);
}