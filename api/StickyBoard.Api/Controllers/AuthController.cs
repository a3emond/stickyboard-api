using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Auth;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
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
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return Ok(result);
        }

        // ------------------------------------------------------------------
        // LOGIN
        // ------------------------------------------------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(result);
        }

        // ------------------------------------------------------------------
        // REFRESH TOKEN
        // ------------------------------------------------------------------
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RefreshAsync(dto.RefreshToken, ct);
            return Ok(result);
        }

        // ------------------------------------------------------------------
        // LOGOUT (Revoke refresh tokens)
        // ------------------------------------------------------------------
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _authService.LogoutAsync(userId, ct);
            return Ok(new { success = true });
        }

        // ------------------------------------------------------------------
        // ME (Authenticated user shortcut)
        // ------------------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var me = await _authService.GetMeAsync(userId, ct);
            return me is null ? NotFound() : Ok(me);
        }
    }
}
