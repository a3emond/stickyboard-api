using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    // ----------------------------------------------------------------------
    // REGISTER
    // ----------------------------------------------------------------------
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<AuthLoginResponse>>> Register(
        [FromBody] RegisterRequestDto dto, 
        CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto.Email, dto.Password, dto.DisplayName, dto.InviteToken, ct);
        return Ok(ApiResponseDto<AuthLoginResponse>.Ok(result));
    }

    // ----------------------------------------------------------------------
    // LOGIN
    // ----------------------------------------------------------------------
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<AuthLoginResponse>>> Login(
        [FromBody] AuthLoginRequest dto, 
        CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);
        return Ok(ApiResponseDto<AuthLoginResponse>.Ok(result));
    }

    // ----------------------------------------------------------------------
    // REFRESH TOKEN
    // ----------------------------------------------------------------------
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<AuthRefreshResponse>>> Refresh(
        [FromBody] AuthRefreshRequest dto,
        CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(dto, ct);
        return Ok(ApiResponseDto<AuthRefreshResponse>.Ok(result));
    }


    // ----------------------------------------------------------------------
    // LOGOUT (Revoke all refresh tokens)
    // ----------------------------------------------------------------------
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

    // ----------------------------------------------------------------------
    // ME (Current authenticated user)
    // ----------------------------------------------------------------------
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
    
    // ----------------------------------------------------------------------
    // CLEANUP REVOKED TOKENS (Admin / Maintenance)
    // ----------------------------------------------------------------------
    [HttpPost("cleanup")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponseDto<object>>> CleanupRevokedTokens(CancellationToken ct)
    {
        var deleted = await _auth.CleanupRevokedTokensAsync(ct);
        return Ok(ApiResponseDto<object>.Ok(new { deleted }));
    }
}
