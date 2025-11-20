using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class ViewRepository : RepositoryBase<View>, IViewRepository
{
    public ViewRepository(NpgsqlDataSource db) : base(db) { }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(View e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO views
            (id, board_id, title, type, layout, position, version)
            VALUES
            (@id, @board_id, @title, @type, @layout, @position, @version)
            RETURNING id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        var id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id;

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("board_id", e.BoardId);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("type", e.Type.ToString());
        cmd.Parameters.AddWithValue("position", e.Position);
        cmd.Parameters.AddWithValue("version", e.Version);

        cmd.Parameters.Add("layout", NpgsqlDbType.Jsonb)
            .Value = e.Layout?.RootElement.GetRawText() ?? "{}";

        return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
    }

    // ---------------------------------------------------------------------
    // UPDATE (VERSION ENFORCED)
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(View e, CancellationToken ct)
    {
        var sql = $@"
            UPDATE views
            SET title    = @title,
                type     = @type,
                layout   = @layout,
                position = @position
            WHERE {ConcurrencyWhere(e)}
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("type", e.Type.ToString());
        cmd.Parameters.AddWithValue("position", e.Position);

        cmd.Parameters.Add("layout", NpgsqlDbType.Jsonb)
            .Value = e.Layout?.RootElement.GetRawText() ?? "{}";

        BindConcurrencyParameters(cmd, e);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // GET FOR BOARD
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<View>> GetForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var sql = ApplySoftDelete(@"
            SELECT *
            FROM views
            WHERE board_id = @board_id
            ORDER BY position ASC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}