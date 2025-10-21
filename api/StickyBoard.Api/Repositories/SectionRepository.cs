using Npgsql;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class SectionRepository : RepositoryBase<Section>
    {
        public SectionRepository(string connectionString) : base(connectionString) { }

        protected override Section Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Section>(reader);

        public override async Task<Guid> CreateAsync(Section e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO sections (board_id, title, position, layout_meta)
                VALUES (@board, @title, @pos, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("pos", e.Position);
            cmd.Parameters.AddWithValue("meta", e.LayoutMeta.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Section e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE sections SET
                    title=@title,
                    position=@pos,
                    layout_meta=@meta,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("pos", e.Position);
            cmd.Parameters.AddWithValue("meta", e.LayoutMeta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM sections WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
