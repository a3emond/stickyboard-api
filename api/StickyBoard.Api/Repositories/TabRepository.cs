using Npgsql;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class TabRepository : RepositoryBase<Tab>
    {
        public TabRepository(NpgsqlDataSource connectionString) : base(connectionString) { }

        protected override Tab Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Tab>(reader);

        public override async Task<Guid> CreateAsync(Tab e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO tabs (scope, board_id, section_id, title, tab_type, layout_config, position)
                VALUES (@scope, @board, @section, @title, @type, @config, @pos)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("scope", e.Scope.ToString());
            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("section", (object?)e.SectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("type", e.TabType);
            cmd.Parameters.AddWithValue("config", e.LayoutConfig.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("pos", e.Position);

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Tab e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE tabs SET
                    title=@title,
                    layout_config=@config,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("config", e.LayoutConfig.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM tabs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
