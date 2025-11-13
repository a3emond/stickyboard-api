using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.UsersAndAuth;
using StickyBoard.Api.Services.UsersAndAuth;
using StickyBoard.Api.Services.UsersAndAuth.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // ============================================================
    // REGISTER
    // ============================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<AuthResultDto>>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _auth.RegisterAsync(dto, ct);
            return Ok(ApiResponseDto<AuthResultDto>.Ok(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponseDto<AuthResultDto>.Fail(ex.Message));
        }
    }

    // ============================================================
    // LOGIN
    // ============================================================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<AuthResultDto>>> Login(
        [FromBody] LoginDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(dto, ct);
            return Ok(ApiResponseDto<AuthResultDto>.Ok(result));
        }
        catch (AuthInvalidException ex)
        {
            return Unauthorized(ApiResponseDto<AuthResultDto>.Fail(ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponseDto<AuthResultDto>.Fail(ex.Message));
        }
    }

    // ============================================================
    // REFRESH TOKEN
    // ============================================================
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<RefreshTokenResultDto>>> Refresh(
        [FromBody] RefreshTokenRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _auth.RefreshAsync(dto, ct);
            return Ok(ApiResponseDto<RefreshTokenResultDto>.Ok(result));
        }
        catch (AuthExpiredException ex)
        {
            return Unauthorized(ApiResponseDto<RefreshTokenResultDto>.Fail(ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponseDto<RefreshTokenResultDto>.Fail(ex.Message));
        }
    }

    // ============================================================
    // LOGOUT (Revoke all tokens for this user)
    // ============================================================
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponseDto<object>>> Logout(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        await _auth.LogoutAsync(userId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ============================================================
    // ME (Current authenticated user)
    // ============================================================
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponseDto<UserSelfDto>>> Me(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<UserSelfDto>.Fail("Invalid or missing token."));

        var me = await _auth.GetSelfAsync(userId, ct);
        if (me is null)
            return NotFound(ApiResponseDto<UserSelfDto>.Fail("User not found."));

        return Ok(ApiResponseDto<UserSelfDto>.Ok(me));
    }

    // ============================================================
    // CLEANUP REVOKED TOKENS (Admin only)
    // ============================================================
    [HttpPost("cleanup")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponseDto<object>>> CleanupRevokedTokens(CancellationToken ct)
    {
        var deleted = await _auth.CleanupRevokedTokensAsync(ct);
        return Ok(ApiResponseDto<object>.Ok(new { deleted }));
    }
}
