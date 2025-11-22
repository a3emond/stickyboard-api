using Npgsql;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class MentionRepository
    : RepositoryBase<Mention>, IMentionRepository
{
    public MentionRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(Mention e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO mentions (entity_type, entity_id, mentioned_user, author_id)
            VALUES (@et, @eid, @mu, @au)
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("et", e.EntityType);
        cmd.Parameters.AddWithValue("eid", e.EntityId);
        cmd.Parameters.AddWithValue("mu", e.MentionedUser);
        cmd.Parameters.AddWithValue("au", (object?)e.AuthorId ?? DBNull.Value);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override Task<bool> UpdateAsync(Mention e, CancellationToken ct)
    {
        throw new NotSupportedException("Mentions are immutable.");
    }

    public async Task<IEnumerable<Mention>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM mentions
             WHERE mentioned_user = @uid
             ORDER BY created_at DESC;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Mention>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }

    public async Task<IEnumerable<Mention>> GetForEntityAsync(EntityType entityType, Guid entityId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM mentions
             WHERE entity_type = @et
               AND entity_id   = @eid
             ORDER BY created_at ASC;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("et", entityType);
        cmd.Parameters.AddWithValue("eid", entityId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Mention>();
        while (await r.ReadAsync(ct))
            list.Add(MapRow(r));

        return list;
    }
}
