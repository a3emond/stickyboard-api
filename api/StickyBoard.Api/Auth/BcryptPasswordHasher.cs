using System.Security.Cryptography;
using System.Text;

namespace StickyBoard.Api.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    // Deterministic (no salt) hash for refresh tokens
    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}