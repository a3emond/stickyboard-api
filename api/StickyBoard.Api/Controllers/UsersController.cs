using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.UsersAndAuth;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services;
using StickyBoard.Api.Services.UsersAndAuth;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    // ------------------------------------------------------------
    // GET CURRENT USER
    // ------------------------------------------------------------
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponseDto<UserSelfDto>>> GetMe(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<UserSelfDto>.Fail("Invalid or missing token."));

        var me = await _users.GetSelfAsync(userId, ct);
        return me is not null
            ? Ok(ApiResponseDto<UserSelfDto>.Ok(me))
            : NotFound(ApiResponseDto<UserSelfDto>.Fail("User not found."));
    }

    // ------------------------------------------------------------
    // UPDATE PROFILE (self)
    // ------------------------------------------------------------
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateProfile([FromBody] UserUpdateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _users.UpdateProfileAsync(userId, dto, ct);
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

        await _users.ChangePasswordAsync(userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // SEARCH USERS (display name)
    // ------------------------------------------------------------
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<UserDto>>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(ApiResponseDto<IEnumerable<UserDto>>.Fail("Query parameter 'q' is required."));

        var list = await _users.SearchAsync(q, ct);
        return Ok(ApiResponseDto<IEnumerable<UserDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // ADMIN: GET USER BY ID
    // ------------------------------------------------------------
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponseDto<UserDto>>> GetById(Guid id, CancellationToken ct)
    {
        var u = await _users.GetAsync(id, ct);
        return u is not null
            ? Ok(ApiResponseDto<UserDto>.Ok(u))
            : NotFound(ApiResponseDto<UserDto>.Fail("User not found."));
    }

    // ------------------------------------------------------------
    // ADMIN: UPDATE ROLE
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateRole(Guid id, [FromQuery] UserRole role, CancellationToken ct)
    {
        var ok = await _users.UpdateRoleAsync(id, role, ct);
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
        var ok = await _users.DeleteAsync(id, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("User not found."));
    }
}
