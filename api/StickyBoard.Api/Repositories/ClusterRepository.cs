using Npgsql;
using System.Text.Json;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class ClusterRepository : RepositoryBase<Cluster>
    {
        public ClusterRepository(string connectionString) : base(connectionString) { }

        protected override Cluster Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Cluster>(reader);

        public override async Task<Guid> CreateAsync(Cluster e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO clusters (board_id, cluster_type, rule_def, visual_meta)
                VALUES (@board, @type, @rule, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("type", e.ClusterType.ToString());
            cmd.Parameters.AddWithValue("rule", (object?)e.RuleDef?.RootElement.GetRawText() ?? "{}");
            cmd.Parameters.AddWithValue("meta", e.VisualMeta.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Cluster e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE clusters SET
                    rule_def=@rule,
                    visual_meta=@meta,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("rule", (object?)e.RuleDef?.RootElement.GetRawText() ?? "{}");
            cmd.Parameters.AddWithValue("meta", e.VisualMeta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM clusters WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
