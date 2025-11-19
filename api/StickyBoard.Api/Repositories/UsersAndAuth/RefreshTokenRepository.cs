using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.UsersAndAuth.Contracts;
using NpgsqlTypes;

namespace StickyBoard.Api.Repositories.UsersAndAuth;

public sealed class RefreshTokenRepository 
    : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(NpgsqlDataSource db) : base(db) { }

    protected override RefreshToken MapRow(NpgsqlDataReader r)
        => MappingHelper.MapEntity<RefreshToken>(r);

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(RefreshToken e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO refresh_tokens (
                token_hash, user_id, client_id, user_agent, ip_addr,
                expires_at, revoked, issued_at
            )
            VALUES (@hash, @uid, @client, @agent, @ip, @exp, FALSE, NOW());
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("hash", e.TokenHash);
        cmd.Parameters.AddWithValue("uid", e.UserId);
        cmd.Parameters.AddWithValue("client", (object?)e.ClientId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("agent", (object?)e.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ip", (object?)e.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("exp", e.ExpiresAt);

        await cmd.ExecuteNonQueryAsync(ct);

        // PK is token_hash → always Guid.Empty
        return Guid.Empty;
    }

    // ---------------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(RefreshToken e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE refresh_tokens
            SET revoked     = @rev,
                revoked_at  = CASE 
                                 WHEN @rev = TRUE AND revoked = FALSE 
                                 THEN NOW() 
                                 ELSE revoked_at 
                              END,
                replaced_by = @replaced
            WHERE token_hash = @hash;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("hash", e.TokenHash);
        cmd.Parameters.AddWithValue("rev", e.Revoked);
        cmd.Parameters.AddWithValue("replaced", (object?)e.ReplacedBy ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // DELETE OVERRIDE (security: hard delete)
    // ---------------------------------------------------------------------
    public async Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = "DELETE FROM refresh_tokens WHERE user_id = @uid;";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("uid", userId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // GET BY HASH
    // ---------------------------------------------------------------------
    public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
            FROM refresh_tokens
            WHERE token_hash = @hash
              AND revoked = FALSE
              AND expires_at > NOW()
            LIMIT 1;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("hash", hash);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? MapRow(r) : null;
    }

    // ---------------------------------------------------------------------
    // REVOKE SINGLE TOKEN
    // ---------------------------------------------------------------------
    public async Task<bool> RevokeTokenAsync(string hash, CancellationToken ct)
    {
        const string sql = @"
            UPDATE refresh_tokens
            SET revoked    = TRUE,
                revoked_at = NOW()
            WHERE token_hash = @hash;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("hash", hash);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // REVOKE ALL TOKENS FOR USER
    // ---------------------------------------------------------------------
    public async Task<bool> RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE refresh_tokens
            SET revoked    = TRUE,
                revoked_at = NOW()
            WHERE user_id = @uid;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("uid", userId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // GET ALL FOR USER
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
            FROM refresh_tokens
            WHERE user_id = @uid
            ORDER BY issued_at DESC;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<RefreshToken>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }

    // ---------------------------------------------------------------------
    // CLEANUP REVOKED
    // ---------------------------------------------------------------------
    public async Task<int> CleanupRevokedAsync(CancellationToken ct)
    {
        const string sql = "DELETE FROM refresh_tokens WHERE revoked = TRUE;";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
