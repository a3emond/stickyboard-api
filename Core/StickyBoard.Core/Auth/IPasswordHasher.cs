// Auth/PasswordHasher.cs

namespace StickyBoard.Core.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);

    // Deterministic hash for refresh tokens
    string HashToken(string token);
}