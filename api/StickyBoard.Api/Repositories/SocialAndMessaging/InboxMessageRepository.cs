using Npgsql;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class InboxMessageRepository
    : RepositoryBase<InboxMessage>, IInboxMessageRepository
{
    public InboxMessageRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(InboxMessage e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO inbox_messages (sender_id, receiver_id, content)
            VALUES (@s, @r, @c)
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("s", (object?)e.SenderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("r", e.ReceiverId);
        cmd.Parameters.AddWithValue("c", e.Content);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(InboxMessage e, CancellationToken ct)
    {
        throw new NotSupportedException("Inbox messages are immutable. Use MarkAsReadAsync.");
    }

    public async Task<IEnumerable<InboxMessage>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM inbox_messages
             WHERE receiver_id = @uid
             ORDER BY created_at DESC;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<InboxMessage>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }

    public async Task<bool> MarkAsReadAsync(Guid messageId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE inbox_messages
               SET read_at = now()
             WHERE id = @id
               AND read_at IS NULL;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", messageId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }
}
