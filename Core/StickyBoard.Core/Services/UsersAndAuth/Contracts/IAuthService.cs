using StickyBoard.Core.DTOs.UsersAndAuth;

namespace StickyBoard.Core.Services.UsersAndAuth.Contracts;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct);
    Task<RefreshTokenResultDto> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken ct);
    Task<bool> LogoutAsync(Guid userId, CancellationToken ct);
    Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct);
    Task<int> CleanupRevokedTokensAsync(CancellationToken ct);
}