using System.Security.Cryptography;
using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.UsersAndAuth;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.UsersAndAuth;
using StickyBoard.Api.Auth;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Repositories.UsersAndAuth.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.UsersAndAuth.Contracts;

namespace StickyBoard.Api.Services.UsersAndAuth;

public sealed class AuthService : IAuthService
{
    private const int RefreshTokenDays = 30;

    private readonly IUserRepository _users;
    private readonly IAuthUserRepository _auth;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IInviteService _invites;

    public AuthService(
        IUserRepository users,
        IAuthUserRepository auth,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IInviteService invites)
    {
        _users = users;
        _auth = auth;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
        _invites = invites;
    }

    // ============================================================
    // REGISTER
    // ============================================================
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password) ||
            string.IsNullOrWhiteSpace(dto.DisplayName))
            throw new ValidationException("Missing required fields.");

        var existing = await _users.GetByEmailAsync(dto.Email, ct);
        if (existing is not null)
            throw new ValidationException("Email already registered.");

        var user = new User
        {
            Email       = dto.Email.Trim(),
            DisplayName = dto.DisplayName.Trim(),
            Prefs       = "{}".ToJsonDocument(),
            Groups      = Array.Empty<string>()
        };

        var userId = await _users.CreateAsync(user, ct);

        var au = new AuthUser
        {
            UserId      = userId,
            PasswordHash = _hasher.Hash(dto.Password),
            Role        = UserRole.user,
            LastLogin   = DateTime.UtcNow
        };
        await _auth.CreateAsync(au, ct);

        // ============================================================
        // INVITE ACCEPT LOGIC (correct structure)
        // ============================================================
        if (dto is IInviteAware invite && !string.IsNullOrWhiteSpace(invite.InviteToken))
        {
            var acceptRequest = new InviteAcceptRequestDto
            {
                Token          = invite.InviteToken!,
                AcceptingUserId = userId
            };

            var acceptResult = await _invites.AcceptAsync(acceptRequest, ct);

            // acceptResult may be null if expired or invalid
            if (acceptResult == null)
                throw new ValidationException("Invalid or expired invite token.");

            // If this was a contact invite, the DB function has already inserted reciprocal contacts.
            // If it was a workspace or board invite, membership is already created.
        }

        // ============================================================
        // ISSUE TOKENS
        // ============================================================
        var (access, refresh) = await IssueTokensAsync(userId, dto.Email, au.Role, ct);

        var created = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found after registration.");

        return new AuthResultDto
        {
            AccessToken  = access,
            RefreshToken = refresh,
            User = ToSelfDto(created, au)
        };
    }


    // ============================================================
    // LOGIN
    // ============================================================
    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ValidationException("Email and password are required.");

        var user = await _users.GetByEmailAsync(dto.Email, ct)
            ?? throw new AuthInvalidException("Invalid credentials.");

        var au = await _auth.GetByUserIdAsync(user.Id, ct)
            ?? throw new AuthInvalidException("Invalid credentials.");

        if (!_hasher.Verify(dto.Password, au.PasswordHash))
            throw new AuthInvalidException("Invalid credentials.");

        await _auth.UpdateLastLoginAsync(user.Id, ct);

        var (access, refresh) = await IssueTokensAsync(user.Id, user.Email, au.Role, ct);

        return new AuthResultDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            User = ToSelfDto(user, au)
        };
    }

    // ============================================================
    // REFRESH TOKEN
    // ============================================================
    public async Task<RefreshTokenResultDto> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new ValidationException("Refresh token is required.");

        var hashed = _hasher.HashToken(dto.RefreshToken);

        var token = await _refreshTokens.GetByHashAsync(hashed, ct)
            ?? throw new AuthExpiredException("Invalid or expired refresh token.");

        var user = await _users.GetByIdAsync(token.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        var au = await _auth.GetByUserIdAsync(user.Id, ct)
            ?? throw new AuthInvalidException("Auth record missing.");

        var (access, newRefresh) = await IssueTokensAsync(user.Id, user.Email, au.Role, ct);

        return new RefreshTokenResultDto
        {
            AccessToken = access,
            RefreshToken = newRefresh
        };
    }

    // ============================================================
    // LOGOUT
    // ============================================================
    public async Task<bool> LogoutAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return true;

        await _refreshTokens.RevokeAllAsync(userId, ct);
        return true;
    }

    // ============================================================
    // SELF PROFILE
    // ============================================================
    public async Task<UserSelfDto?> GetSelfAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return null;

        var user = await _users.GetByIdAsync(userId, ct);
        var au = await _auth.GetByUserIdAsync(userId, ct);

        return user is null || au is null ? null : ToSelfDto(user, au);
    }

    // ============================================================
    // CLEANUP REVOKED TOKENS
    // ============================================================
    public async Task<int> CleanupRevokedTokensAsync(CancellationToken ct)
        => await _refreshTokens.CleanupRevokedAsync(ct);

    // ============================================================
    // INTERNAL HELPERS
    // ============================================================
    private async Task<(string access, string refreshRaw)> IssueTokensAsync(
        Guid userId,
        string email,
        UserRole role,
        CancellationToken ct)
    {
        await _refreshTokens.RevokeAllAsync(userId, ct);

        var access = _jwt.CreateToken(userId, email, role.ToString());
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var hash = _hasher.HashToken(raw);

        var rt = new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            Revoked = false,
            ClientId = null,
            UserAgent = null,
            IpAddress = null
        };

        await _refreshTokens.CreateAsync(rt, ct);
        return (access, raw);
    }

    private static UserSelfDto ToSelfDto(User u, AuthUser au) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        AvatarUrl = u.AvatarUri,
        Prefs = u.Prefs,
        Groups = u.Groups ?? [],
        CreatedAt = u.CreatedAt
    };
}
