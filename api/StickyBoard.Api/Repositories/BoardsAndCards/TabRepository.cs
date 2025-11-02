using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public class TabRepository : RepositoryBase<Tab>
{
    public TabRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override Tab Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<Tab>(r);

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public override async Task<Guid> CreateAsync(Tab e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO tabs (
                board_id, title, tab_type, layout_config, position
            )
            VALUES (
                @board, @title, @type, @config, @pos
            )
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("board", e.BoardId);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("type", e.TabType);
        cmd.Parameters.Add("config", NpgsqlDbType.Jsonb)
            .Value = e.LayoutConfig?.RootElement.GetRawText() ?? "{}";
        cmd.Parameters.AddWithValue("pos", e.Position);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    public override async Task<bool> UpdateAsync(Tab e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE tabs
            SET title = @title,
                tab_type = @type,
                layout_config = @config,
                position = @pos,
                updated_at = now()
            WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("type", e.TabType);
        cmd.Parameters.Add("config", NpgsqlDbType.Jsonb)
            .Value = e.LayoutConfig?.RootElement.GetRawText() ?? "{}";
        cmd.Parameters.AddWithValue("pos", e.Position);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ------------------------------------------------------------
    // READ — All tabs for a board
    // ------------------------------------------------------------
    public async Task<IEnumerable<Tab>> GetByBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = new List<Tab>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM tabs
            WHERE board_id = @board AND deleted_at IS NULL
            ORDER BY position ASC;", conn);

        cmd.Parameters.AddWithValue("board", boardId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // READ — Filter by tab type
    // ------------------------------------------------------------
    public async Task<IEnumerable<Tab>> GetByTypeAsync(TabType type, CancellationToken ct)
    {
        var list = new List<Tab>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM tabs
            WHERE tab_type = @type AND deleted_at IS NULL
            ORDER BY board_id, position ASC;", conn);

        cmd.Parameters.AddWithValue("type", type);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // REORDER
    // ------------------------------------------------------------
    public async Task<int> ReorderAsync(Guid boardId, IEnumerable<(Guid Id, int Pos)> positions, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var total = 0;
        foreach (var (id, pos) in positions)
        {
            await using var cmd = new NpgsqlCommand(@"
                UPDATE tabs
                SET position = @pos, updated_at = now()
                WHERE id = @id AND board_id = @board", conn, tx);

            cmd.Parameters.AddWithValue("board", boardId);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("pos", pos);

            total += await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return total;
    }

    public async Task<bool> MoveAsync(Guid tabId, Guid tabBoardId, int newPosition, CancellationToken ct)
    {
        // Load all tabs for board
        var tabs = await GetByBoardAsync(tabBoardId, ct);

        if (!tabs.Any(t => t.Id == tabId))
            return false;

        // Rebuild sorted order
        var ordered = tabs
            .OrderBy(t => t.Position)
            .ToList();

        var moving = ordered.First(t => t.Id == tabId);
        ordered.Remove(moving);

        // Clamp to valid range
        newPosition = Math.Max(0, Math.Min(newPosition, ordered.Count));

        ordered.Insert(newPosition, moving);

        // Apply new incremental positions
        var updates = ordered
            .Select((t, i) => (t.Id, Pos: i))
            .ToList();

        await ReorderAsync(tabBoardId, updates, ct);
        return true;
    }


    public async Task<int> GetMaxPositionAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
        SELECT COALESCE(MAX(position), -1)
        FROM tabs
        WHERE board_id=@board AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("board", boardId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

}
