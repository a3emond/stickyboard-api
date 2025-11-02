using System.Security.Cryptography;
using System.Text.Json;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.UsersAndAuth;

namespace StickyBoard.Api.Services;

public sealed class AuthService
{
    // Match DB default 'now() + interval ''30 days''' for refresh tokens
    private const int RefreshTokenDays = 30;

    private readonly UserRepository _users;
    private readonly AuthUserRepository _auth;
    private readonly RefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly InviteService _inviteService;

    public AuthService(
        UserRepository users,
        AuthUserRepository auth,
        RefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        InviteService inviteService)
    {
        _users = users;
        _auth = auth;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
        _inviteService = inviteService;
    }

    // ----------------------------------------------------------------------
    // REGISTER
    // NOTE: DTOs for Register aren't in the core contract; we keep it simple
    // and return the same payload shape as Login (AuthLoginResponse).
    // ----------------------------------------------------------------------
    public async Task<AuthLoginResponse> RegisterAsync(
        string email,
        string password,
        string displayName,
        string? inviteToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException("Password is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ValidationException("Display name is required.");

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ValidationException("Email already registered.");

        // Create user
        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            AvatarUri = null,
            Prefs = JsonDocument.Parse("{}")
        };
        var userId = await _users.CreateAsync(user, ct);

        // Create auth record
        var au = new AuthUser
        {
            UserId = userId,
            PasswordHash = _hasher.Hash(password),
            Role = UserRole.user,
            LastLogin = DateTime.UtcNow
        };
        await _auth.CreateAsync(au, ct);

        // Optional: redeem invite
        if (!string.IsNullOrWhiteSpace(inviteToken))
            await _inviteService.RedeemAsync(userId, inviteToken!, ct);

        // Tokens
        var (accessToken, refreshRaw) = await IssueTokensAsync(userId, email, au.Role, ct);

        // Return Login-shape response with full self profile
        var createdUser = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found after registration.");

        return new AuthLoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshRaw,
            User = ToSelfDto(createdUser)
        };
    }

    // ----------------------------------------------------------------------
    // LOGIN
    // ----------------------------------------------------------------------
    public async Task<AuthLoginResponse> LoginAsync(AuthLoginRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ValidationException("Email and password are required.");

        var user = await _users.GetByEmailAsync(dto.Email, ct);
        if (user is null)
            throw new AuthInvalidException("Invalid credentials");

        var au = await _auth.GetByUserIdAsync(user.Id, ct);
        if (au is null || !_hasher.Verify(dto.Password, au.PasswordHash))
            throw new AuthInvalidException("Invalid credentials");

        await _auth.UpdateLastLoginAsync(user.Id, ct);

        var (accessToken, refreshRaw) = await IssueTokensAsync(user.Id, user.Email, au.Role, ct);

        return new AuthLoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshRaw,
            User = ToSelfDto(user)
        };
    }

    // ----------------------------------------------------------------------
    // REFRESH TOKEN
    // ----------------------------------------------------------------------
    public async Task<AuthRefreshResponse> RefreshAsync(AuthRefreshRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new ValidationException("Refresh token is required.");

        // Hash incoming raw token
        var hashed = _hasher.Hash(dto.RefreshToken);

        // Direct lookup by hashed token (revoked + expiry handled in repo)
        var token = await _refreshTokens.GetByHashAsync(hashed, ct);
        if (token is null)
            throw new AuthExpiredException("Invalid or expired refresh token.");

        // Rotate: revoke old, issue new
        token.Revoked = true;
        await _refreshTokens.UpdateAsync(token, ct);

        // Load user and auth record
        var user = await _users.GetByIdAsync(token.UserId, ct)
                   ?? throw new NotFoundException("User not found.");

        var au = await _auth.GetByUserIdAsync(user.Id, ct)
                 ?? throw new AuthInvalidException("Auth record missing.");

        // Issue new pair
        var (access, newRefreshRaw) = await IssueTokensAsync(user.Id, user.Email, au.Role, ct);

        return new AuthRefreshResponse
        {
            AccessToken = access,
            RefreshToken = newRefreshRaw
        };
    }


    // ----------------------------------------------------------------------
    // LOGOUT (revoke all refresh tokens for the user)
    // ----------------------------------------------------------------------
    public async Task<bool> LogoutAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return true;
        await _refreshTokens.RevokeAllAsync(userId, ct);
        return true;
    }

    // ----------------------------------------------------------------------
    // ME / SELF PROFILE
    // ----------------------------------------------------------------------
    public async Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return null;
        var user = await _users.GetByIdAsync(userId, ct);
        return user is null ? null : ToSelfDto(user);
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------
    private async Task<(string access, string refreshRaw)> IssueTokensAsync(
        Guid userId, string email, UserRole role, CancellationToken ct)
    {
        var access = _jwt.CreateToken(userId, email, role.ToString());

        // 256-bit random token, hex-encoded
        var refreshRaw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var refreshHash = _hasher.Hash(refreshRaw);

        var rt = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            Revoked = false
        };
        await _refreshTokens.CreateAsync(rt, ct);

        return (access, refreshRaw);
    }

    private static UserSelfDto ToSelfDto(User u) => new UserSelfDto
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        AvatarUrl = u.AvatarUri,
        Prefs = u.Prefs,
        CreatedAt = u.CreatedAt
    };
}
