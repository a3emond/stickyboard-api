using Npgsql;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class SectionRepository : RepositoryBase<Section>
    {
        public SectionRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Section Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Section>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Section e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO sections (board_id, title, position, layout_meta)
                VALUES (@board, @title, @pos, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("pos", e.Position);
            cmd.Parameters.AddWithValue("meta", e.LayoutMeta.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Section e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE sections SET
                    title = @title,
                    position = @pos,
                    layout_meta = @meta,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("pos", e.Position);
            cmd.Parameters.AddWithValue("meta", e.LayoutMeta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM sections WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve all sections for a specific board
        public async Task<IEnumerable<Section>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Section>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM sections
                WHERE board_id = @board
                ORDER BY position ASC", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve section by title (useful for imports / matching)
        public async Task<Section?> GetByTitleAsync(Guid boardId, string title, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM sections
                WHERE board_id = @board
                  AND LOWER(title) = LOWER(@title)
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("board", boardId);
            cmd.Parameters.AddWithValue("title", title);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        // Bulk reorder sections for a given board
        public async Task<int> ReorderAsync(Guid boardId, IEnumerable<(Guid Id, int Position)> positions, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var totalUpdated = 0;
            foreach (var (id, pos) in positions)
            {
                await using var cmd = new NpgsqlCommand(@"
                    UPDATE sections
                    SET position = @pos,
                        updated_at = now()
                    WHERE id = @id AND board_id = @board", conn, tx);

                cmd.Parameters.AddWithValue("board", boardId);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("pos", pos);

                totalUpdated += await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return totalUpdated;
        }

        // Delete all sections belonging to a given board (used for board reset)
        public async Task<int> DeleteByBoardAsync(Guid boardId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM sections WHERE board_id=@board", conn);
            cmd.Parameters.AddWithValue("board", boardId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
