// Services/AuthService.cs

using StickyBoard.Api.DTOs.Auth;

namespace StickyBoard.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct);
        Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct);
    }
}
