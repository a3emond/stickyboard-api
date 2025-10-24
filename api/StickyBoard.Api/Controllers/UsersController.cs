using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Users;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IAuthService _authService;

        public UsersController(UserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // ------------------------------------------------------------------
        // CURRENT USER
        // ------------------------------------------------------------------
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var me = await _authService.GetMeAsync(userId, ct);
            return me is null ? NotFound() : Ok(me);
        }

        // ------------------------------------------------------------------
        // UPDATE PROFILE (self)
        // ------------------------------------------------------------------
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _userService.UpdateProfileAsync(userId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        // ------------------------------------------------------------------
        // CHANGE PASSWORD (self)
        // ------------------------------------------------------------------
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _userService.ChangePasswordAsync(userId, dto, ct);
            return Ok(new { success = true });
        }

        // ------------------------------------------------------------------
        // SEARCH USERS (collaborator selection)
        // ------------------------------------------------------------------
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Query parameter 'q' is required." });

            var users = await _userService.SearchAsync(q, ct);
            return Ok(users);
        }

        // ------------------------------------------------------------------
        // ADMIN: UPDATE ROLE
        // ------------------------------------------------------------------
        [HttpPut("{id:guid}/role")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromQuery] UserRole role, CancellationToken ct)
        {
            var ok = await _userService.UpdateRoleAsync(id, role, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        // ------------------------------------------------------------------
        // ADMIN: DELETE USER
        // ------------------------------------------------------------------
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _userService.DeleteAsync(id, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
