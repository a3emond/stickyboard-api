using Npgsql;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.RealTime;

namespace StickyBoard.Core.Repositories.Automation.RealTime;

public sealed class EventOutboxRepository : IEventOutboxRepository
{
    private readonly NpgsqlDataSource _db;

    public EventOutboxRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    // READ BATCH (ordered by cursor)
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<EventOutbox>> GetBatchAsync(
        long afterCursor,
        int limit,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT
                               cursor,
                               topic,
                               entity_id,
                               workspace_id,
                               board_id,
                               op,
                               payload::text AS payload,
                               created_at
                           FROM event_outbox
                           WHERE cursor > @after
                           ORDER BY cursor ASC
                           LIMIT @limit;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("after", afterCursor);
        cmd.Parameters.AddWithValue("limit", limit);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<EventOutbox>();
        while (await r.ReadAsync(ct))
            list.Add(new EventOutbox
            {
                Cursor = r.GetInt64(0),
                Topic = r.GetFieldValue<OutboxTopic>(1),
                EntityId = r.GetGuid(2),
                WorkspaceId = r.IsDBNull(3) ? null : r.GetGuid(3),
                BoardId = r.IsDBNull(4) ? null : r.GetGuid(4),
                Op = r.GetFieldValue<OutboxOperation>(5),
                Payload = r.GetString(6),
                CreatedAt = r.GetDateTime(7)
            });

        return list;
    }

    // ------------------------------------------------------------
    // PURGE (called after successful processing + push)
    // ------------------------------------------------------------
    public async Task PurgeUpToAsync(long cursor, CancellationToken ct)
    {
        const string sql = """
                           DELETE FROM event_outbox
                           WHERE cursor <= @cursor;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("cursor", cursor);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
    {
        return _db.OpenConnectionAsync(ct);
    }
}