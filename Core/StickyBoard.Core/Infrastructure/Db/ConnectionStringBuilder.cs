// StickyBoard.Core/Infrastructure/Db/ConnectionStringBuilder.cs

using Microsoft.Extensions.Configuration;

namespace StickyBoard.Core.Infrastructure.Db;

public static class ConnectionStringBuilder
{
    public static string Build(IConfiguration config)
    {
        var dbHost = config["DB_HOST"] ?? "localhost";
        var dbUser = config["POSTGRES_USER"];
        var dbPass = config["POSTGRES_PASSWORD"];
        var dbName = config["POSTGRES_DB"];

        var dbUrl = config["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(dbUrl) &&
            dbUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(dbUrl);
            var userInfo = uri.UserInfo.Split(':', 2);

            var user = userInfo.Length > 0 ? userInfo[0] : string.Empty;
            var pass = userInfo.Length > 1 ? userInfo[1] : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            return
                $"Host={host};Port={port};Database={database};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
        }

        return
            $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Prefer;Trust Server Certificate=true";
    }
}