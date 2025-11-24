namespace StickyBoard.Core.Auth;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, string role);
}