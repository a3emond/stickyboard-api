using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class BoardRepository : RepositoryBase<Board>, IBoardRepository
{
    public BoardRepository(NpgsqlDataSource db) : base(db) { }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(Board e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO boards
            (id, workspace_id, title, theme, meta, created_by)
            VALUES
            (@id, @workspace_id, @title, @theme, @meta, @created_by)
            RETURNING id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        var id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id;

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("workspace_id", e.WorkspaceId);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("created_by", (object?)e.CreatedBy ?? DBNull.Value);

        cmd.Parameters.Add("theme", NpgsqlDbType.Jsonb)
            .Value = e.Theme?.RootElement.GetRawText() ?? "{}";

        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
    }

    // ---------------------------------------------------------------------
    // UPDATE (concurrency-safe)
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(Board e, CancellationToken ct)
    {
        var sql = $@"
            UPDATE boards
            SET title = @title,
                theme = @theme,
                meta = @meta
            WHERE {ConcurrencyWhere(e)}
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("title", e.Title);

        cmd.Parameters.Add("theme", NpgsqlDbType.Jsonb)
            .Value = e.Theme?.RootElement.GetRawText() ?? "{}";

        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        BindConcurrencyParameters(cmd, e);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // GET FOR WORKSPACE
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Board>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var sql = ApplySoftDelete(@"
            SELECT *
            FROM boards
            WHERE workspace_id = @workspace_id
            ORDER BY updated_at DESC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}
