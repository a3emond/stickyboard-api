using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Auth;
using StickyBoard.Api.Services;

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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(result);
        }
    }
}