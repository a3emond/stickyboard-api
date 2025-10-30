using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class CardRepository : RepositoryBase<Card>
    {
        public CardRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Card Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Card>(r);

        // ---------------------------------------------------------------------
        // CORE CRUD
        // ---------------------------------------------------------------------

         public override async Task<Guid> CreateAsync(Card e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO cards (
                    board_id, section_id, tab_id, type, title, content, ink_data,
                    due_date, start_time, end_time, priority, status,
                    created_by, assignee_id, version
                )
                VALUES (
                    @board, @section, @tab, @type, @title, @content, @ink,
                    @due, @start, @end, @prio, @status,
                    @creator, @assignee, @ver
                )
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("section", (object?)e.SectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tab", (object?)e.TabId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("type", e.Type);
            cmd.Parameters.AddWithValue("title", (object?)e.Title ?? DBNull.Value);

            cmd.Parameters.AddWithValue("content", NpgsqlDbType.Jsonb, e.Content.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("ink", e.InkData is null 
                ? DBNull.Value 
                : (object)new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = e.InkData.RootElement.GetRawText() });

            cmd.Parameters.AddWithValue("due", (object?)e.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("start", (object?)e.StartTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("end", (object?)e.EndTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("prio", (object?)e.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("status", e.Status);
            cmd.Parameters.AddWithValue("creator", (object?)e.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("assignee", (object?)e.AssigneeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ver", e.Version);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Card e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE cards SET
                    section_id = @section,
                    tab_id = @tab,
                    title = @title,
                    content = @content,
                    ink_data = @ink,
                    due_date = @due,
                    start_time = @start,
                    end_time = @end,
                    priority = @prio,
                    status = @status,
                    assignee_id = @assignee,
                    version = version + 1,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("section", (object?)e.SectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tab", (object?)e.TabId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("title", (object?)e.Title ?? DBNull.Value);

            cmd.Parameters.AddWithValue("content", NpgsqlDbType.Jsonb, e.Content.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("ink", e.InkData is null 
                ? DBNull.Value 
                : (object)new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = e.InkData.RootElement.GetRawText() });

            cmd.Parameters.AddWithValue("due", (object?)e.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("start", (object?)e.StartTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("end", (object?)e.EndTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("prio", (object?)e.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("status", e.Status);
            cmd.Parameters.AddWithValue("assignee", (object?)e.AssigneeId ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM cards WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ---------------------------------------------------------------------
        // RETRIEVAL QUERIES
        // ---------------------------------------------------------------------

        public async Task<Card?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM cards WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public async Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM cards WHERE board_id=@b ORDER BY created_at ASC", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Card>> GetBySectionAsync(Guid sectionId, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM cards WHERE section_id=@s ORDER BY created_at ASC", conn);
            cmd.Parameters.AddWithValue("s", sectionId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Card>> GetByTabAsync(Guid tabId, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM cards WHERE tab_id=@t ORDER BY created_at ASC", conn);
            cmd.Parameters.AddWithValue("t", tabId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Card>> GetByAssigneeAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM cards WHERE assignee_id=@u ORDER BY due_date NULLS LAST", conn);
            cmd.Parameters.AddWithValue("u", userId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ---------------------------------------------------------------------
        // FILTERS AND SEARCH
        // ---------------------------------------------------------------------

        public async Task<IEnumerable<Card>> SearchAsync(Guid boardId, string keyword, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM cards
                WHERE board_id=@b AND
                      (LOWER(title) LIKE LOWER(@kw)
                       OR (content ->> 'recognizedText') ILIKE @kw)
                ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("kw", $"%{keyword}%");
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Card>> GetByStatusAsync(Guid boardId, string status, CancellationToken ct)
        {
            var list = new List<Card>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM cards WHERE board_id=@b AND status=@s ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("s", status);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ---------------------------------------------------------------------
        // BULK OPERATIONS
        // ---------------------------------------------------------------------

        public async Task<int> BulkDeleteBySectionAsync(Guid sectionId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM cards WHERE section_id=@s", conn);
            cmd.Parameters.AddWithValue("s", sectionId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<int> BulkReassignSectionAsync(Guid oldSectionId, Guid newSectionId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE cards SET section_id=@new, updated_at=now()
                WHERE section_id=@old", conn);
            cmd.Parameters.AddWithValue("old", oldSectionId);
            cmd.Parameters.AddWithValue("new", newSectionId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<int> BulkReassignTabAsync(Guid oldTabId, Guid newTabId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE cards SET tab_id=@new, updated_at=now()
                WHERE tab_id=@old", conn);
            cmd.Parameters.AddWithValue("old", oldTabId);
            cmd.Parameters.AddWithValue("new", newTabId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<int> BulkAssignUserAsync(Guid boardId, Guid userId, IEnumerable<Guid> cardIds, CancellationToken ct)
        {
            if (!cardIds.Any()) return 0;

            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var total = 0;
            foreach (var cardId in cardIds)
            {
                await using var cmd = new NpgsqlCommand(@"
                    UPDATE cards
                    SET assignee_id=@u, updated_at=now()
                    WHERE id=@id AND board_id=@b", conn, tx);

                cmd.Parameters.AddWithValue("b", boardId);
                cmd.Parameters.AddWithValue("u", userId);
                cmd.Parameters.AddWithValue("id", cardId);
                total += await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return total;
        }
    }
}
