using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using NpgsqlTypes;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

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
                @sender, @email, @board, @org,
                @boardRole, @orgRole,
                @token, @accepted, @expires
            )
            RETURNING id", conn);

        cmd.Parameters.AddWithValue("sender", e.SenderId);
        cmd.Parameters.AddWithValue("email", e.Email);
        cmd.Parameters.AddWithValue("board", (object?)e.BoardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("org", (object?)e.OrgId ?? DBNull.Value);

        cmd.Parameters.AddWithValue("boardRole",
            e.BoardRole.HasValue ? e.BoardRole.Value : (object)DBNull.Value);

        cmd.Parameters.AddWithValue("orgRole",
            e.OrgRole.HasValue ? e.OrgRole.Value : (object)DBNull.Value);

        cmd.Parameters.AddWithValue("token", e.Token);
        cmd.Parameters.AddWithValue("accepted", e.Accepted);
        cmd.Parameters.AddWithValue("expires", e.ExpiresAt);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Invite e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE invites
            SET accepted=@accepted
            WHERE id=@id", conn);

        cmd.Parameters.AddWithValue("accepted", e.Accepted);
        cmd.Parameters.AddWithValue("id", e.Id);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM invites WHERE id=@id", conn);

        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<Invite?> GetByTokenAsync(string token, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM invites
            WHERE token=@token
              AND expires_at > NOW()", conn);

        cmd.Parameters.AddWithValue("token", token);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IEnumerable<Invite>> GetPendingForEmailAsync(string email, CancellationToken ct)
    {
        var list = new List<Invite>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM invites
            WHERE LOWER(email) = LOWER(@email)
              AND accepted = FALSE
              AND expires_at > NOW()", conn);

        cmd.Parameters.AddWithValue("email", email);
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
            WHERE sender_id=@sender
              AND accepted = FALSE
              AND expires_at > NOW()
            ORDER BY created_at DESC", conn);

        cmd.Parameters.AddWithValue("sender", senderId);
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
              AND sender_id=@sender
              AND accepted = FALSE
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("id", inviteId);
        cmd.Parameters.AddWithValue("sender", senderId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null;
    }
}
