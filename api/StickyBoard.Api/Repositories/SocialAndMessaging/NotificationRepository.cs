using Npgsql;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class NotificationRepository
    : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(Notification e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO notifications (user_id, type, entity_id)
            VALUES (@u, @t, @eid)
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("u", (object?)e.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("t", e.Type);
        cmd.Parameters.AddWithValue("eid", (object?)e.EntityId ?? DBNull.Value);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Notification e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE notifications
               SET read   = @rd,
                   read_at = @ra
             WHERE id = @id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("rd", e.Read);
        cmd.Parameters.AddWithValue("ra", (object?)e.ReadAt ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<Notification>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct)
    {
        var sql = @"
            SELECT *
              FROM notifications
             WHERE user_id = @uid
        ";

        if (unreadOnly)
            sql += " AND read = FALSE";

        sql += " ORDER BY created_at DESC;";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Notification>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE notifications
               SET read   = TRUE,
                   read_at = now()
             WHERE id      = @id
               AND user_id = @uid
               AND read    = FALSE;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notificationId);
        cmd.Parameters.AddWithValue("uid", userId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE notifications
               SET read   = TRUE,
                   read_at = now()
             WHERE user_id = @uid
               AND read    = FALSE;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);

        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
