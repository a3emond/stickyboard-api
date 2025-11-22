using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class MessageRepository : RepositoryBase<Message>, IMessageRepository
{
    public MessageRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(Message e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO messages (channel, board_id, view_id, sender_id, content, parent_id)
            VALUES (@ch, @b, @v, @s, @c, @p)
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("ch", e.Channel);
        cmd.Parameters.AddWithValue("b", (object?)e.BoardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("v", (object?)e.ViewId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("s", (object?)e.SenderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("c", e.Content);
        cmd.Parameters.AddWithValue("p", (object?)e.ParentId ?? DBNull.Value);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Message e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE messages
               SET content = @c,
                   parent_id = @p
             WHERE id = @id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("c", e.Content);
        cmd.Parameters.AddWithValue("p", (object?)e.ParentId ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<Message>> GetByBoardAsync(Guid boardId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
              FROM messages
             WHERE board_id = @bid
             ORDER BY created_at ASC;
        ");

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bid", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Message>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }

    public async Task<IEnumerable<Message>> GetByViewAsync(Guid viewId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
              FROM messages
             WHERE view_id = @vid
             ORDER BY created_at ASC;
        ");

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("vid", viewId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Message>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }
}
