using Npgsql;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class CardTagRepository : RepositoryBase<CardTag>
    {
        public CardTagRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override CardTag Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<CardTag>(r);

        // ----------------------------------------------------------------------
        // CREATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(CardTag e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO card_tags (card_id, tag_id)
                VALUES (@card, @tag)
                RETURNING card_id", conn);

            cmd.Parameters.AddWithValue("card", e.CardId);
            cmd.Parameters.AddWithValue("tag", e.TagId);

            await cmd.ExecuteScalarAsync(ct);
            return e.CardId;
        }

        public override Task<bool> UpdateAsync(CardTag e, CancellationToken ct)
        {
            // Junction table — no update operation
            return Task.FromResult(false);
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            // "id" represents the card_id; deletes all tags linked to a card
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM card_tags WHERE card_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<CardTag>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<CardTag>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM card_tags WHERE card_id=@c", conn);
            cmd.Parameters.AddWithValue("c", cardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        public async Task<IEnumerable<CardTag>> GetByTagAsync(Guid tagId, CancellationToken ct)
        {
            var list = new List<CardTag>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM card_tags WHERE tag_id=@t", conn);
            cmd.Parameters.AddWithValue("t", tagId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        public async Task<bool> RemoveTagAsync(Guid cardId, Guid tagId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "DELETE FROM card_tags WHERE card_id=@c AND tag_id=@t", conn);
            cmd.Parameters.AddWithValue("c", cardId);
            cmd.Parameters.AddWithValue("t", tagId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }
    }
}
