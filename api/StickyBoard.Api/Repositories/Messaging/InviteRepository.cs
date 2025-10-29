using Npgsql;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.Messaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class InviteRepository : RepositoryBase<Invite>
    {
        public InviteRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Invite Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Invite>(r);

        public override async Task<Guid> CreateAsync(Invite e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO invites (
                    sender_id, email, board_id, org_id,
                    board_role, org_role,
                    token, accepted, expires_at
                )
                VALUES (
                    @s, @em, @b, @o,
                    @br, @or,
                    @t, @a, @x
                )
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("s", e.SenderId);
            cmd.Parameters.AddWithValue("em", e.Email);
            cmd.Parameters.AddWithValue("b", (object?)e.BoardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("o", (object?)e.OrgId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("br", (object?)e.BoardRole ?? DBNull.Value);
            cmd.Parameters.AddWithValue("or", (object?)e.OrgRole ?? DBNull.Value);
            cmd.Parameters.AddWithValue("t", e.Token);
            cmd.Parameters.AddWithValue("a", e.Accepted);
            cmd.Parameters.AddWithValue("x", e.ExpiresAt);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Invite e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE invites
                SET accepted=@a
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("a", e.Accepted);
            cmd.Parameters.AddWithValue("id", e.Id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM invites WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<Invite?> GetByTokenAsync(string token, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM invites WHERE token=@t", conn);
            cmd.Parameters.AddWithValue("t", token);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public async Task<IEnumerable<Invite>> GetPendingForEmailAsync(string email, CancellationToken ct)
        {
            var list = new List<Invite>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM invites
                WHERE LOWER(email)=LOWER(@em)
                  AND accepted=false
                  AND expires_at>now()", conn);

            cmd.Parameters.AddWithValue("em", email);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Invite>> GetPendingBySenderAsync(Guid senderId, CancellationToken ct)
        {
            var list = new List<Invite>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM invites
                WHERE sender_id=@s
                  AND accepted=false
                  AND expires_at>now()
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("s", senderId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<bool> CancelIfOwnedAsync(Guid senderId, Guid inviteId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM invites
                WHERE id=@id
                  AND sender_id=@s
                  AND accepted=false
                RETURNING id;", conn);

            cmd.Parameters.AddWithValue("id", inviteId);
            cmd.Parameters.AddWithValue("s", senderId);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result is not null;
        }
    }
}
