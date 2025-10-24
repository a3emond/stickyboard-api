using Npgsql;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class TabRepository : RepositoryBase<Tab>
    {
        public TabRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Tab Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Tab>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Tab e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
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

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Tab e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE tabs SET
                    title = @title,
                    layout_config = @config,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("config", e.LayoutConfig.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM tabs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve all tabs for a given section
        public async Task<IEnumerable<Tab>> GetBySectionAsync(Guid sectionId, CancellationToken ct)
        {
            var list = new List<Tab>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tabs
                WHERE section_id = @section
                ORDER BY position ASC", conn);

            cmd.Parameters.AddWithValue("section", sectionId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all tabs for a given board (regardless of section)
        public async Task<IEnumerable<Tab>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Tab>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tabs
                WHERE board_id = @board
                ORDER BY section_id, position ASC", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all tabs of a given type (useful for admin analytics)
        public async Task<IEnumerable<Tab>> GetByTypeAsync(string tabType, CancellationToken ct)
        {
            var list = new List<Tab>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM tabs
                WHERE tab_type = @type
                ORDER BY board_id, position ASC", conn);

            cmd.Parameters.AddWithValue("type", tabType);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Bulk reorder tabs within a section
        public async Task<int> ReorderAsync(Guid sectionId, IEnumerable<(Guid Id, int Position)> positions, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var totalUpdated = 0;
            foreach (var (id, pos) in positions)
            {
                await using var cmd = new NpgsqlCommand(@"
                    UPDATE tabs
                    SET position = @pos,
                        updated_at = now()
                    WHERE id = @id AND section_id = @section", conn, tx);

                cmd.Parameters.AddWithValue("section", sectionId);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("pos", pos);

                totalUpdated += await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return totalUpdated;
        }

        // Delete all tabs under a given section (for section cleanup)
        public async Task<int> DeleteBySectionAsync(Guid sectionId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM tabs WHERE section_id=@section", conn);
            cmd.Parameters.AddWithValue("section", sectionId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
