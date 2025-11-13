namespace StickyBoard.Api.Common;

public static class InviteUrlBuilder
{
    // Base URL for all invites
    private const string BaseUrl = "https://stickyboard.aedev.pro/invite";

    /// <summary>
    /// Builds the public invite URL containing the RAW token.
    /// </summary>
    public static string BuildInviteUrl(string rawToken)
    {
        return $"{BaseUrl}?token={Uri.EscapeDataString(rawToken)}";
    }

    /// <summary>
    /// Optional: platform-specific deep link (handled by clients).
    /// </summary>
    public static string BuildDeepLink(string rawToken)
    {
        return $"stickyboard://invite?token={Uri.EscapeDataString(rawToken)}";
    }

    /// <summary>
    /// Optional: fallback page for mobile apps.
    /// Opens on web, then sends user to store.
    /// </summary>
    public static string BuildMobileLandingPageUrl(string rawToken)
    {
        return $"{BaseUrl}/mobile?token={Uri.EscapeDataString(rawToken)}";
    }
}