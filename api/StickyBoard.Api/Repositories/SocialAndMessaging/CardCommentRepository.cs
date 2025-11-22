using Npgsql;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class CardCommentRepository 
    : RepositoryBase<CardComment>, ICardCommentRepository
{
    public CardCommentRepository(NpgsqlDataSource db) : base(db) { }

    public override async Task<Guid> CreateAsync(CardComment entity, CancellationToken ct)
    {
        var sql = $@"
            INSERT INTO {Table}
            (id, card_id, parent_id, user_id, content)
            VALUES
            (gen_random_uuid(), @card_id, @parent_id, @user_id, @content)
            RETURNING id";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("card_id",  entity.CardId);
        cmd.Parameters.AddWithValue("parent_id", (object?)entity.ParentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("user_id",   (object?)entity.UserId   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("content",   entity.Content);

        return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
    }

    public override async Task<bool> UpdateAsync(CardComment entity, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter($@"
            UPDATE {Table}
            SET content = @content
            WHERE {ConcurrencyWhere(entity)}");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", entity.Id);
        cmd.Parameters.AddWithValue("content", entity.Content);

        BindConcurrencyParameters(cmd, entity);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<CardComment>> GetByCardIdAsync(Guid cardId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter($@"
            SELECT * FROM {Table}
            WHERE card_id = @card_id
            ORDER BY created_at ASC");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("card_id", cardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<IEnumerable<CardComment>> GetThreadAsync(Guid cardId, Guid rootCommentId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter($@"
            WITH RECURSIVE thread AS (
                SELECT *
                FROM {Table}
                WHERE id = @root_id

                UNION ALL

                SELECT c.*
                FROM {Table} c
                INNER JOIN thread t ON c.parent_id = t.id
            )
            SELECT *
            FROM thread
            WHERE card_id = @card_id
            ORDER BY created_at ASC");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("card_id", cardId);
        cmd.Parameters.AddWithValue("root_id", rootCommentId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}
