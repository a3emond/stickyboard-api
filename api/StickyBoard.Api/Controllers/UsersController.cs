using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Users;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IAuthService _authService;

        public UsersController(UserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // ------------------------------------------------------------
        // GET CURRENT USER
        // ------------------------------------------------------------
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> GetMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<UserDto>.Fail("Invalid or missing token."));

            var me = await _authService.GetMeAsync(userId, ct);
            return me is not null
                ? Ok(ApiResponseDto<UserDto>.Ok(me))
                : NotFound(ApiResponseDto<UserDto>.Fail("User not found."));
        }

        // ------------------------------------------------------------
        // UPDATE PROFILE (self)
        // ------------------------------------------------------------
        [HttpPut("me")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateProfile([FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _userService.UpdateProfileAsync(userId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("User not found."));
        }

        // ------------------------------------------------------------
        // CHANGE PASSWORD (self)
        // ------------------------------------------------------------
        [HttpPut("me/password")]
        public async Task<ActionResult<ApiResponseDto<object>>> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            await _userService.ChangePasswordAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { success = true }));
        }

        // ------------------------------------------------------------
        // SEARCH USERS
        // ------------------------------------------------------------
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<UserDto>>>> Search([FromQuery] string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponseDto<IEnumerable<UserDto>>.Fail("Query parameter 'q' is required."));

            var users = await _userService.SearchAsync(q, ct);
            return Ok(ApiResponseDto<IEnumerable<UserDto>>.Ok(users));
        }

        // ------------------------------------------------------------
        // ADMIN: UPDATE ROLE
        // ------------------------------------------------------------
        [HttpPut("{id:guid}/role")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateRole(Guid id, [FromQuery] UserRole role, CancellationToken ct)
        {
            var ok = await _userService.UpdateRoleAsync(id, role, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("User not found."));
        }

        // ------------------------------------------------------------
        // ADMIN: DELETE USER
        // ------------------------------------------------------------
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _userService.DeleteAsync(id, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("User not found."));
        }
    }
}
