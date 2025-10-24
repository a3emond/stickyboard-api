using StickyBoard.Api.DTOs.Auth;
using StickyBoard.Api.DTOs.Users;

namespace StickyBoard.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct);
        Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct);
        Task<bool> LogoutAsync(Guid userId, CancellationToken ct);
        Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct); // nullable
    }
}