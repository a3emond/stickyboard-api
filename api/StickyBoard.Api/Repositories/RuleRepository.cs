using Npgsql;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class RuleRepository : RepositoryBase<Rule>
    {
        public RuleRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Rule Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Rule>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Rule e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO rules (board_id, definition, enabled)
                VALUES (@board, @def, @enabled)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("def", e.Definition.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("enabled", e.Enabled);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Rule e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE rules SET
                    definition = @def,
                    enabled = @enabled,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("def", e.Definition.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("enabled", e.Enabled);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM rules WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Get all rules for a given board
        public async Task<IEnumerable<Rule>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Rule>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM rules
                WHERE board_id = @board
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get all enabled rules (for background worker evaluation)
        public async Task<IEnumerable<Rule>> GetEnabledAsync(CancellationToken ct)
        {
            var list = new List<Rule>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM rules
                WHERE enabled = TRUE
                ORDER BY board_id, created_at DESC", conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get all disabled rules (useful for admin filtering)
        public async Task<IEnumerable<Rule>> GetDisabledAsync(CancellationToken ct)
        {
            var list = new List<Rule>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM rules
                WHERE enabled = FALSE
                ORDER BY board_id, created_at DESC", conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get rules for a specific cluster (if linked)
        public async Task<IEnumerable<Rule>> GetByClusterAsync(Guid clusterId, CancellationToken ct)
        {
            var list = new List<Rule>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT r.*
                FROM rules r
                JOIN clusters c ON c.rule_def->>'id' = r.id::text
                WHERE c.id = @cid", conn);

            cmd.Parameters.AddWithValue("cid", clusterId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
