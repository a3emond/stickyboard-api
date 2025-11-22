using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class CardRepository : RepositoryBase<Card>, ICardRepository
{
    public CardRepository(NpgsqlDataSource db) : base(db) { }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(Card e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO cards (
              id, board_id, title, markdown, ink_data,
              due_date, start_date, end_date,
              checklist, priority, status, tags,
              assignee, created_by, last_edited_by
            )
            VALUES (
              @id, @board_id, @title, @markdown, @ink_data,
              @due_date, @start_date, @end_date,
              @checklist, @priority, @status, @tags,
              @assignee, @created_by, @last_edited_by
            )
            RETURNING id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        var id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id;

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("board_id", e.BoardId);
        cmd.Parameters.AddWithValue("title", (object?)e.Title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("markdown", e.Markdown);

        var inkParam = cmd.Parameters.Add("ink_data", NpgsqlDbType.Jsonb);
        inkParam.Value = e.InkData == null
            ? DBNull.Value
            : e.InkData.RootElement.GetRawText();

        cmd.Parameters.AddWithValue("due_date", (object?)e.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("start_date", (object?)e.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("end_date", (object?)e.EndDate ?? DBNull.Value);

        var checklistParam = cmd.Parameters.Add("checklist", NpgsqlDbType.Jsonb);
        checklistParam.Value = e.Checklist == null
            ? DBNull.Value
            : e.Checklist.RootElement.GetRawText();

        cmd.Parameters.AddWithValue("priority", (object?)e.Priority ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", e.Status.ToString());
        cmd.Parameters.AddWithValue("tags", e.Tags ?? Array.Empty<string>());
        cmd.Parameters.AddWithValue("assignee", (object?)e.Assignee ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_by", (object?)e.CreatedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("last_edited_by", (object?)e.LastEditedBy ?? DBNull.Value);

        return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
    }


    // ---------------------------------------------------------------------
    // UPDATE (version + concurrency safe)
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(Card e, CancellationToken ct)
    {
        var sql = $@"
            UPDATE cards
            SET 
              title = @title,
              markdown = @markdown,
              ink_data = @ink_data,
              due_date = @due_date,
              start_date = @start_date,
              end_date = @end_date,
              checklist = @checklist,
              priority = @priority,
              status = @status,
              tags = @tags,
              assignee = @assignee,
              last_edited_by = @last_edited_by
            WHERE {ConcurrencyWhere(e)}
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("title", (object?)e.Title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("markdown", e.Markdown);

        var inkParam = cmd.Parameters.Add("ink_data", NpgsqlDbType.Jsonb);
        inkParam.Value = e.InkData == null
            ? DBNull.Value
            : e.InkData.RootElement.GetRawText();

        cmd.Parameters.AddWithValue("due_date", (object?)e.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("start_date", (object?)e.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("end_date", (object?)e.EndDate ?? DBNull.Value);

        var checklistParam = cmd.Parameters.Add("checklist", NpgsqlDbType.Jsonb);
        checklistParam.Value = e.Checklist == null
            ? DBNull.Value
            : e.Checklist.RootElement.GetRawText();

        cmd.Parameters.AddWithValue("priority", (object?)e.Priority ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", e.Status.ToString());
        cmd.Parameters.AddWithValue("tags", e.Tags ?? Array.Empty<string>());
        cmd.Parameters.AddWithValue("assignee", (object?)e.Assignee ?? DBNull.Value);
        cmd.Parameters.AddWithValue("last_edited_by", (object?)e.LastEditedBy ?? DBNull.Value);

        BindConcurrencyParameters(cmd, e);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }


    // ---------------------------------------------------------------------
    // GET BY BOARD
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
            FROM cards
            WHERE board_id = @board_id
            ORDER BY updated_at DESC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("board_id", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }


    // ---------------------------------------------------------------------
    // SEARCH IN BOARD
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Card>> SearchAsync(Guid boardId, string q, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
            FROM cards
            WHERE board_id = @board_id
              AND (title ILIKE @q OR markdown ILIKE @q)
            ORDER BY updated_at DESC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("q", $"%{q}%");

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}