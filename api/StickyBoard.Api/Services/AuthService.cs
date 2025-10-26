using System.Text.Json;
using StickyBoard.Api.Auth;
using StickyBoard.Api.DTOs.Auth;
using StickyBoard.Api.DTOs.Users;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Users;
using StickyBoard.Api.Repositories;

namespace StickyBoard.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserRepository _users;
    private readonly AuthUserRepository _auth;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly RefreshTokenRepository _refreshTokens;
    private readonly InviteService _inviteService;

    public AuthService(
        UserRepository users,
        AuthUserRepository auth,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        RefreshTokenRepository refreshTokens,
        InviteService inviteService)
    {
        _users = users;
        _auth = auth;
        _hasher = hasher;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
        _inviteService = inviteService;
    }



    // ----------------------------------------------------------------------
    // REGISTER
    // ----------------------------------------------------------------------
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct)
    {
        // 1) Ensure the email is not already registered
        var existing = await _users.GetByEmailAsync(dto.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered.");

        // 2) Create user profile
        var user = new User
        {
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            AvatarUri = null,
            Prefs = JsonDocument.Parse("{}")
        };

        var userId = await _users.CreateAsync(user, ct);
        
        // 3b) Redeem invite if provided
        if (!string.IsNullOrWhiteSpace(dto.InviteToken))
        {
            await _inviteService.RedeemInviteAsync(userId, dto.InviteToken, ct);
        }


        // 3) Create auth record with hashed password
        var au = new AuthUser
        {
            Id = userId,
            PasswordHash = _hasher.Hash(dto.Password),
            Role = UserRole.user
        };

        await _auth.CreateAsync(au, ct);

        // 4) Generate access token
        var token = _jwt.CreateToken(userId, dto.Email, au.Role.ToString());

        // 5) Create refresh token
        var rawRefresh = Guid.NewGuid().ToString("N");
        var hash = _hasher.Hash(rawRefresh);

        var rt = new RefreshToken
        {
            TokenHash = hash,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.CreateAsync(rt, ct);

        // 6) Return combined response
        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = rawRefresh,
            User = new UserDto
            {
                Id = userId,
                Email = dto.Email,
                DisplayName = dto.DisplayName,
                AvatarUri = null,
                Role = au.Role
            }
        };
    }

    // ----------------------------------------------------------------------
    // LOGIN
    // ----------------------------------------------------------------------
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct)
    {
        // 1) Find user by email
        var user = await _users.GetByEmailAsync(dto.Email, ct);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        // 2) Get the corresponding auth record
        var auth = await _auth.GetByUserIdAsync(user.Id, ct);
        if (auth is null || !_hasher.Verify(dto.Password, auth.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");
        // Update last login timestamp
        auth.LastLogin = DateTime.UtcNow;
        await _auth.UpdateAsync(auth, ct);


        // 3) Create short-lived access token
        var token = _jwt.CreateToken(user.Id, user.Email, auth.Role.ToString());

        // 4) Issue refresh token
        var rawRefresh = Guid.NewGuid().ToString("N");
        var hash = _hasher.Hash(rawRefresh);

        var rt = new RefreshToken
        {
            TokenHash = hash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokens.CreateAsync(rt, ct);

        // 5) Return full payload
        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = rawRefresh,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUri = user.AvatarUri,
                Role = auth.Role
            }
        };
    }

    // ----------------------------------------------------------------------
    // REFRESH TOKEN FLOW
    // ----------------------------------------------------------------------
    public async Task<AuthResponseDto> RefreshAsync(Guid userId,string refreshToken, CancellationToken ct)
    {
        // 1) Fetch all refresh tokens (small set) and verify hash match
        var all = await _refreshTokens.GetByUserIdAsync(userId, ct);
        var match = all.FirstOrDefault(t => _hasher.Verify(refreshToken, t.TokenHash));


        if (match is null || match.ExpiresAt < DateTime.UtcNow || match.Revoked)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // 2) Rotate old token
        match.Revoked = true;
        await _refreshTokens.UpdateAsync(match, ct);

        // 3) Load user and auth records
        var user = await _users.GetByIdAsync(match.UserId, ct);
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        var auth = await _auth.GetByUserIdAsync(user.Id, ct);
        if (auth is null)
            throw new UnauthorizedAccessException("Invalid auth record.");

        // 4) Create new tokens
        var newAccess = _jwt.CreateToken(user.Id, user.Email, auth.Role.ToString());
        var newRaw = Guid.NewGuid().ToString("N");
        var newHash = _hasher.Hash(newRaw);

        var newRt = new RefreshToken
        {
            TokenHash = newHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _refreshTokens.CreateAsync(newRt, ct);

        // 5) Return updated session payload
        return new AuthResponseDto
        {
            Token = newAccess,
            RefreshToken = newRaw,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUri = user.AvatarUri,
                Role = auth.Role
            }
        };
    }

    // ----------------------------------------------------------------------
    // LOGOUT (Revoke all refresh tokens)
    // ----------------------------------------------------------------------
    public async Task<bool> LogoutAsync(Guid userId, CancellationToken ct)
    {
        await _refreshTokens.RevokeAllAsync(userId, ct);
        return true;
    }

    // ----------------------------------------------------------------------
    // ME (Authenticated user)
    // ----------------------------------------------------------------------
    public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        var auth = await _auth.GetByUserIdAsync(user.Id, ct);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUri = user.AvatarUri,
            Role = auth?.Role ?? UserRole.user
        };
    }
}
