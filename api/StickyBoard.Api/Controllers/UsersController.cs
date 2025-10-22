using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var uid = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null || !Guid.TryParse(uid, out var userId))
                return Unauthorized();

            var me = await _authService.GetMeAsync(userId, ct);
            return me is null ? NotFound() : Ok(me);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "admin")]
        public IActionResult GetAdminOnly()
        {
            return Ok(new { message = "Welcome admin!" });
        }
    }
}