using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SectionsAndTabs
{
    public class TabRepository : RepositoryBase<Tab>
    {
        public TabRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Tab Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Tab>(reader);

        public override async Task<Guid> CreateAsync(Tab e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO tabs (scope, board_id, section_id, title, tab_type, layout_config, position)
                VALUES (@scope, @board, @section, @title, @type, @config, @pos)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("scope", e.Scope);
            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("section", (object?)e.SectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("type", e.TabType);
            cmd.Parameters.AddWithValue("config", NpgsqlDbType.Jsonb, e.LayoutConfig.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("pos", e.Position);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Tab e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE tabs SET
                    title = @title,
                    tab_type = @type,
                    layout_config = @config,
                    position = @pos,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("type", e.TabType);
            cmd.Parameters.AddWithValue("config", NpgsqlDbType.Jsonb, e.LayoutConfig.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("pos", e.Position);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM tabs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

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

        public async Task<int> ReorderAsync(Guid parentId, IEnumerable<(Guid Id, int Position)> positions, CancellationToken ct)
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
                    WHERE id = @id AND (board_id = @parent OR section_id = @parent)", conn, tx);

                cmd.Parameters.AddWithValue("parent", parentId);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("pos", pos);

                totalUpdated += await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return totalUpdated;
        }

        public async Task<int> DeleteBySectionAsync(Guid sectionId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM tabs WHERE section_id=@section", conn);
            cmd.Parameters.AddWithValue("section", sectionId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
