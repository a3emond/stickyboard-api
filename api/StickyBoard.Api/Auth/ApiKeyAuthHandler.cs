using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace StickyBoard.Api.Auth
{
    public sealed class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _config;

        public ApiKeyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration config)
            : base(options, logger, encoder, clock)
        {
            _config = config;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Worker-Key", out var providedKey))
                return Task.FromResult(AuthenticateResult.NoResult());

            var configuredKey = _config["WORKER_API_KEY"];
            if (string.IsNullOrWhiteSpace(configuredKey))
                return Task.FromResult(AuthenticateResult.Fail("No worker key configured."));

            if (!configuredKey.Equals(providedKey))
                return Task.FromResult(AuthenticateResult.Fail("Invalid worker key."));

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "WorkerService"),
                new Claim(ClaimTypes.Role, "worker")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}