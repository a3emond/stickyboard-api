// Auth/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace StickyBoard.Api.Auth
{
    public interface IJwtTokenService
    {
        string CreateToken(Guid userId, string email, string role);
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        private readonly SigningCredentials _creds;

        public JwtTokenService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(Guid userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("uid", userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: _creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}