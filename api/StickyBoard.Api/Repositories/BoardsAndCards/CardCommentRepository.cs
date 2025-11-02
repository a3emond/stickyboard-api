using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards
{
    public class CardCommentRepository : RepositoryBase<CardComment>
    {
        public CardCommentRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override CardComment Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<CardComment>(r);

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public override async Task<Guid> CreateAsync(CardComment e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO card_comments (card_id, user_id, content)
                VALUES (@card, @user, @content)
                RETURNING id;", conn);

            cmd.Parameters.AddWithValue("card", e.CardId);
            cmd.Parameters.AddWithValue("user", e.UserId);
            cmd.Parameters.AddWithValue("content", e.Content);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // ------------------------------------------------------------
        // UPDATE CONTENT
        // ------------------------------------------------------------
        public override async Task<bool> UpdateAsync(CardComment e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE card_comments
                   SET content = @content,
                       updated_at = now()
                 WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("content", e.Content);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // SOFT DELETE
        // ------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE card_comments
                   SET deleted_at = now(),
                       updated_at = now()
                 WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }


        // ------------------------------------------------------------
        // GET BY CARD
        // ------------------------------------------------------------
        public async Task<IEnumerable<CardComment>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<CardComment>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM card_comments
                WHERE card_id=@card AND deleted_at IS NULL
                ORDER BY created_at ASC;", conn);

            cmd.Parameters.AddWithValue("card", cardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
