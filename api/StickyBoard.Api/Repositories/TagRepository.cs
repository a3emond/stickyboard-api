using Npgsql;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class TagRepository : RepositoryBase<Tag>
    {
        public TagRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Tag Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Tag>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Tag e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO tags (name)
                VALUES (@name)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("name", e.Name);
            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Tag e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE tags
                SET name = @name,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("name", e.Name);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM tags WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve a tag by name (case-insensitive)
        public async Task<Tag?> GetByNameAsync(string name, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tags
                WHERE LOWER(name) = LOWER(@name)
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("name", name);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        // Retrieve multiple tags by name list (used for card tagging or bulk import)
        public async Task<IEnumerable<Tag>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct)
        {
            var list = new List<Tag>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tags
                WHERE LOWER(name) = ANY(@names)", conn);

            cmd.Parameters.AddWithValue("names", names.Select(n => n.ToLowerInvariant()).ToArray());

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Search tags by partial name match
        public async Task<IEnumerable<Tag>> SearchAsync(string partial, CancellationToken ct)
        {
            var list = new List<Tag>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tags
                WHERE name ILIKE @pattern
                ORDER BY name ASC", conn);

            cmd.Parameters.AddWithValue("pattern", $"%{partial}%");

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
