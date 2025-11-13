// Auth/PasswordHasher.cs

using BCrypt.Net;

namespace StickyBoard.Api.Auth
{

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);

        // Deterministic hash for refresh tokens
        string HashToken(string token);
    }
}