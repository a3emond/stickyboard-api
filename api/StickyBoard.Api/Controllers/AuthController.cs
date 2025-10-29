using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Auth;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Users;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ------------------------------------------------------------------
        // REGISTER
        // ------------------------------------------------------------------
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return Ok(ApiResponseDto<AuthResponseDto>.Ok(result));
        }

        // ------------------------------------------------------------------
        // LOGIN
        // ------------------------------------------------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(ApiResponseDto<AuthResponseDto>.Ok(result));
        }

        // ------------------------------------------------------------------
        // REFRESH TOKEN
        // ------------------------------------------------------------------
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Refresh([FromBody] RefreshRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RefreshAsync(dto.UserId, dto.RefreshToken, ct);
            return Ok(ApiResponseDto<AuthResponseDto>.Ok(result));
        }

        // ------------------------------------------------------------------
        // LOGOUT (Revoke refresh tokens)
        // ------------------------------------------------------------------
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<object>>> Logout(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            await _authService.LogoutAsync(userId, ct);
            return Ok(ApiResponseDto<object>.Ok(new { success = true }));
        }

        // ------------------------------------------------------------------
        // ME (Authenticated user shortcut)
        // ------------------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> Me(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<UserDto>.Fail("Invalid or missing token."));

            var me = await _authService.GetMeAsync(userId, ct);
            if (me is null)
                return NotFound(ApiResponseDto<UserDto>.Fail("User not found."));

            return Ok(ApiResponseDto<UserDto>.Ok(me));
        }
    }
}
