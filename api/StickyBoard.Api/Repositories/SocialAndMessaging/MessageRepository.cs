using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public class MessageRepository : RepositoryBase<Message>
{
    public MessageRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override Message Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<Message>(r);

    public override async Task<Guid> CreateAsync(Message e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO messages (
                sender_id, receiver_id, subject, body,
                type, related_board, related_org, status
            )
            VALUES (
                @sender, @receiver, @subject, @body,
                @type, @board, @org, @status
            )
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("sender", (object?)e.SenderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("receiver", e.ReceiverId);
        cmd.Parameters.AddWithValue("subject", (object?)e.Subject ?? DBNull.Value);
        cmd.Parameters.AddWithValue("body", e.Body);
        cmd.Parameters.AddWithValue("type", e.Type);
        cmd.Parameters.AddWithValue("board", (object?)e.RelatedBoard ?? DBNull.Value);
        cmd.Parameters.AddWithValue("org", (object?)e.RelatedOrg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", e.Status);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Message e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE messages
               SET status=@status,
                   updated_at = now()
             WHERE id=@id", conn);

        cmd.Parameters.AddWithValue("status", e.Status);
        cmd.Parameters.AddWithValue("id", e.Id);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // Uses base soft-delete implementation, don't override unless hard delete needed
    // public override Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    //     => base.DeleteAsync(id, ct);

    public async Task<IEnumerable<Message>> GetInboxAsync(Guid userId, CancellationToken ct)
    {
        var list = new List<Message>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM messages
            WHERE receiver_id=@uid
              AND deleted_at IS NULL
            ORDER BY created_at DESC;", conn);

        cmd.Parameters.AddWithValue("uid", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*)
              FROM messages
            WHERE receiver_id=@uid
              AND deleted_at IS NULL
              AND status = @status;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("status", MessageStatus.unread);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }
}
