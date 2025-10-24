using Npgsql;
using System.Text.Json;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class ClusterRepository : RepositoryBase<Cluster>
    {
        public ClusterRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Cluster Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Cluster>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Cluster e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO clusters (board_id, cluster_type, rule_def, visual_meta)
                VALUES (@board, @type, @rule, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("type", e.ClusterType.ToString());
            cmd.Parameters.AddWithValue("rule", (object?)e.RuleDef?.RootElement.GetRawText() ?? "{}");
            cmd.Parameters.AddWithValue("meta", e.VisualMeta.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Cluster e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE clusters SET
                    rule_def=@rule,
                    visual_meta=@meta,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("rule", (object?)e.RuleDef?.RootElement.GetRawText() ?? "{}");
            cmd.Parameters.AddWithValue("meta", e.VisualMeta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM clusters WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Get all clusters for a specific board (most common use case)
        public async Task<IEnumerable<Cluster>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Cluster>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM clusters
                WHERE board_id = @board
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve clusters by cluster type (e.g., manual vs auto)
        public async Task<IEnumerable<Cluster>> GetByTypeAsync(string clusterType, CancellationToken ct)
        {
            var list = new List<Cluster>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM clusters
                WHERE cluster_type = @type
                ORDER BY board_id, created_at DESC", conn);

            cmd.Parameters.AddWithValue("type", clusterType);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve a single cluster by board and rule definition hash (optional)
        public async Task<Cluster?> GetByRuleSignatureAsync(Guid boardId, string ruleJson, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM clusters
                WHERE board_id = @board
                  AND rule_def::text = @rule
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("board", boardId);
            cmd.Parameters.AddWithValue("rule", ruleJson);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
    }
}
