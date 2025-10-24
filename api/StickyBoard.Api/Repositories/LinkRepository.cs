using Npgsql;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class LinkRepository : RepositoryBase<Link>
    {
        public LinkRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Link Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Link>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Link e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO links (from_card, to_card, rel_type, created_by)
                VALUES (@from, @to, @rel, @creator)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("from", e.FromCard);
            cmd.Parameters.AddWithValue("to", e.ToCard);
            cmd.Parameters.AddWithValue("rel", e.RelType.ToString());
            cmd.Parameters.AddWithValue("creator", (object?)e.CreatedBy ?? DBNull.Value);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Link e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE links
                SET rel_type = @rel,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("rel", e.RelType.ToString());

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM links WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Get all links originating from a given card
        public async Task<IEnumerable<Link>> GetLinksFromAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<Link>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM links
                WHERE from_card = @card
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("card", cardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get all links pointing *to* a given card
        public async Task<IEnumerable<Link>> GetLinksToAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<Link>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM links
                WHERE to_card = @card
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("card", cardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get all links between two cards (useful for bidirectional checks)
        public async Task<Link?> GetBetweenAsync(Guid fromCard, Guid toCard, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM links
                WHERE from_card = @from AND to_card = @to
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("from", fromCard);
            cmd.Parameters.AddWithValue("to", toCard);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        // Get all links of a specific type (optional for analytics)
        public async Task<IEnumerable<Link>> GetByTypeAsync(string relType, CancellationToken ct)
        {
            var list = new List<Link>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM links
                WHERE rel_type = @type
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("type", relType);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
