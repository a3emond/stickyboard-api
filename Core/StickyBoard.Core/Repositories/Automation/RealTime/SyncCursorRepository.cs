using Npgsql;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.RealTime;

namespace StickyBoard.Core.Repositories.Automation.RealTime;

public sealed class SyncCursorRepository : ISyncCursorRepository
{
    private readonly NpgsqlDataSource _db;

    public SyncCursorRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    // GET BY PK
    // ------------------------------------------------------------
    public async Task<SyncCursor?> GetAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT user_id, scope_type, scope_id, last_cursor, updated_at
                           FROM sync_cursor
                           WHERE user_id = @user_id
                             AND scope_type = @scope_type
                             AND scope_id IS NOT DISTINCT FROM @scope_id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("scope_type", scopeType);
        cmd.Parameters.AddWithValue("scope_id", (object?)scopeId ?? DBNull.Value);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        if (!await r.ReadAsync(ct))
            return null;

        return new SyncCursor
        {
            UserId = r.GetGuid(0),
            ScopeType = r.GetFieldValue<SyncScopeType>(1),
            ScopeId = r.IsDBNull(2) ? null : r.GetGuid(2),
            LastCursor = r.GetInt64(3),
            UpdatedAt = r.GetDateTime(4)
        };
    }

    // ------------------------------------------------------------
    // UPSERT
    // ------------------------------------------------------------
    public async Task UpsertAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        long lastCursor,
        CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO sync_cursor (user_id, scope_type, scope_id, last_cursor, updated_at)
                           VALUES (@user_id, @scope_type, @scope_id, @last_cursor, NOW())
                           ON CONFLICT (user_id, scope_type, scope_id)
                           DO UPDATE SET
                               last_cursor = EXCLUDED.last_cursor,
                               updated_at = NOW();
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("scope_type", scopeType);
        cmd.Parameters.AddWithValue("scope_id", (object?)scopeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("last_cursor", lastCursor);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    public async Task<bool> DeleteAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        CancellationToken ct)
    {
        const string sql = """
                           DELETE FROM sync_cursor
                           WHERE user_id = @user_id
                             AND scope_type = @scope_type
                             AND scope_id IS NOT DISTINCT FROM @scope_id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("scope_type", scopeType);
        cmd.Parameters.AddWithValue("scope_id", (object?)scopeId ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
    {
        return _db.OpenConnectionAsync(ct);
    }
}