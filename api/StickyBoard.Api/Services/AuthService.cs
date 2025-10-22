using System.Text.Json;
using StickyBoard.Api.Auth;
using StickyBoard.Api.DTOs.Auth;
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

    public AuthService(UserRepository users, AuthUserRepository auth, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _auth = auth;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct)
    {
        // 1) Create user profile
        var user = new User
        {
            Id = Guid.Empty, // assigned by DB
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            AvatarUri = null,
            Prefs = JsonDocument.Parse("{}")
        };

        var userId = await _users.CreateAsync(user);

        // 2) Create auth record w/ hashed password and default role = 'user'
        var au = new AuthUser
        {
            Id = userId,
            PasswordHash = _hasher.Hash(dto.Password),
            Role = UserRole.user // enum aligned with DB
        };
        await _auth.CreateAsync(au);

        var token = _jwt.CreateToken(userId, dto.Email, au.Role.ToString());
        return new AuthResponseDto
        {
            Token = token,
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

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct)
    {
        // Lookup by email
        var all = await _users.GetAllAsync(); // a quick path until a proper repo method exists
        var user = all.FirstOrDefault(u => string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase));
        if (user is null) throw new UnauthorizedAccessException("Invalid credentials.");

        var auth = await _auth.GetByIdAsync(user.Id);
        if (auth is null || !_hasher.Verify(dto.Password, auth.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = _jwt.CreateToken(user.Id, user.Email, auth.Role.ToString());
        return new AuthResponseDto
        {
            Token = token,
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

    public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return null;
        var auth = await _auth.GetByIdAsync(user.Id);
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