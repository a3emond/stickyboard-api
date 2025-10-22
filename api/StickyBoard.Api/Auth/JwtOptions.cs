// Auth/JwtOptions.cs
namespace StickyBoard.Api.Auth
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 60;
    }
}