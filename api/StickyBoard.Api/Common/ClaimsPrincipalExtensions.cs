// Common/ClaimsPrincipalExtensions.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StickyBoard.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? user.FindFirstValue("uid");
        return Guid.TryParse(id, out var g) ? g : Guid.Empty;
    }

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(JwtRegisteredClaimNames.Email);

    public static string? GetRole(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role);
}