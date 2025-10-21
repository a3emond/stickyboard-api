using Npgsql;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class RuleRepository : RepositoryBase<Rule>
    {
        public RuleRepository(string connectionString) : base(connectionString) { }

        protected override Rule Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Rule>(reader);

        public override async Task<Guid> CreateAsync(Rule e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO rules (board_id, definition, enabled)
                VALUES (@board, @def, @enabled)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("def", e.Definition.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("enabled", e.Enabled);

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Rule e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE rules SET
                    definition=@def,
                    enabled=@enabled,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("def", e.Definition.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("enabled", e.Enabled);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM rules WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}