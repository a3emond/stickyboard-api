using Npgsql;
using StickyBoard.Api.Models.Messaging;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    /// <summary>
    /// Repository for CRUD operations and queries on the messages table.
    /// </summary>
    public class MessageRepository : RepositoryBase<Message>
    {
        public MessageRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Message Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Message>(r);

        // ----------------------------------------------------------------------
        // CREATE
        // ----------------------------------------------------------------------
        public override async Task<Guid> CreateAsync(Message e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO messages (
                    sender_id, receiver_id, subject, body,
                    type, related_board, related_org, status
                )
                VALUES (
                    @s, @r, @sub, @b,
                    @t, @rb, @ro, @st
                )
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("s", (object?)e.SenderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("r", e.ReceiverId);
            cmd.Parameters.AddWithValue("sub", (object?)e.Subject ?? DBNull.Value);
            cmd.Parameters.AddWithValue("b", (object?)e.Body ?? DBNull.Value);
            cmd.Parameters.AddWithValue("t", e.Type);
            cmd.Parameters.AddWithValue("rb", (object?)e.RelatedBoardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ro", (object?)e.RelatedOrganizationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("st", e.Status); 

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // ----------------------------------------------------------------------
        // UPDATE
        // ----------------------------------------------------------------------
        public override async Task<bool> UpdateAsync(Message e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE messages
                SET status=@st
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("st", e.Status); // uses MessageStatus enum
            cmd.Parameters.AddWithValue("id", e.Id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // DELETE
        // ----------------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM messages WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // READ: Inbox messages
        // ----------------------------------------------------------------------
        public async Task<IEnumerable<Message>> GetInboxAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Message>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM messages
                WHERE receiver_id=@r
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("r", userId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ----------------------------------------------------------------------
        // READ: Unread message count
        // ----------------------------------------------------------------------
        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*)
                FROM messages
                WHERE receiver_id=@r AND status=@st", conn);

            cmd.Parameters.AddWithValue("r", userId);
            cmd.Parameters.AddWithValue("st", MessageStatus.unread); // parameterized enum
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
    }
}
