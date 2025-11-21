using Npgsql;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class BoardMemberRepository : IBoardMemberRepository
{
    private readonly NpgsqlDataSource _db;

    public BoardMemberRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
        => _db.OpenConnectionAsync(ct);

    // ---------------------------------------------------------------------
    // ADD OR UPDATE OVERRIDE (PROMOTE / DEMOTE / BLOCK)
    // ---------------------------------------------------------------------
    public async Task<bool> AddOrUpdateAsync(BoardMember entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO board_members (board_id, user_id, role)
            VALUES (@board_id, @user_id, @role)
            ON CONFLICT (board_id, user_id)
            DO UPDATE SET role = EXCLUDED.role
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", entity.BoardId);
        cmd.Parameters.AddWithValue("user_id", entity.UserId);
        cmd.Parameters.AddWithValue("role", entity.Role.ToString());

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // REMOVE OVERRIDE (REVERT TO WORKSPACE ROLE)
    // ---------------------------------------------------------------------
    public async Task<bool> RemoveAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM board_members
            WHERE board_id = @board_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // HAS OVERRIDE (NOT EFFECTIVE ROLE)
    // ---------------------------------------------------------------------
    public async Task<bool> HasOverrideAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM board_members
            WHERE board_id = @board_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    // ---------------------------------------------------------------------
    // EFFECTIVE ROLE (BOARD OVERRIDE → WORKSPACE FALLBACK)
    // ---------------------------------------------------------------------
    public async Task<WorkspaceRole?> GetEffectiveRoleAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT COALESCE(bm.role, wm.role)
            FROM boards b
            JOIN workspace_members wm
                ON wm.workspace_id = b.workspace_id
               AND wm.user_id = @user_id
            LEFT JOIN board_members bm
                ON bm.board_id = b.id
               AND bm.user_id = @user_id
            WHERE b.id = @board_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        var result = await cmd.ExecuteScalarAsync(ct);

        if (result is null)
            return null;

        return Enum.Parse<WorkspaceRole>(result.ToString()!, true);
    }

    // ---------------------------------------------------------------------
    // EFFECTIVE MEMBERS BY BOARD (EXCLUDING BLOCKED)
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetEffectiveByBoardAsync(Guid boardId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                b.id,
                wm.user_id,
                COALESCE(bm.role, wm.role) AS role
            FROM boards b
            JOIN workspace_members wm
                ON wm.workspace_id = b.workspace_id
            LEFT JOIN board_members bm
                ON bm.board_id = b.id
               AND bm.user_id = wm.user_id
            WHERE b.id = @board_id
              AND COALESCE(bm.role, wm.role) <> 'none'
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<BoardMember>();

        while (await r.ReadAsync(ct))
        {
            list.Add(new BoardMember
            {
                BoardId = r.GetGuid(0),
                UserId  = r.GetGuid(1),
                Role    = Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }

    // ---------------------------------------------------------------------
    // EFFECTIVE BOARDS BY USER (EXCLUDING BLOCKED)
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetEffectiveByUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT
                b.id,
                @user_id,
                COALESCE(bm.role, wm.role) AS role
            FROM boards b
            JOIN workspace_members wm
                ON wm.workspace_id = b.workspace_id
               AND wm.user_id = @user_id
            LEFT JOIN board_members bm
                ON bm.board_id = b.id
               AND bm.user_id = @user_id
            WHERE COALESCE(bm.role, wm.role) <> 'none'
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<BoardMember>();

        while (await r.ReadAsync(ct))
        {
            list.Add(new BoardMember
            {
                BoardId = r.GetGuid(0),
                UserId  = userId,
                Role    = Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }

    // ---------------------------------------------------------------------
    // RAW OVERRIDES BY BOARD (ADMIN / DEBUG)
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetOverridesByBoardAsync(Guid boardId, CancellationToken ct)
    {
        const string sql = @"
            SELECT board_id, user_id, role
            FROM board_members
            WHERE board_id = @board_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<BoardMember>();

        while (await r.ReadAsync(ct))
        {
            list.Add(new BoardMember
            {
                BoardId = r.GetGuid(0),
                UserId  = r.GetGuid(1),
                Role    = Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }

    // ---------------------------------------------------------------------
    // RAW OVERRIDES BY USER (ADMIN / DEBUG)
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetOverridesByUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT board_id, user_id, role
            FROM board_members
            WHERE user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var list = new List<BoardMember>();

        while (await r.ReadAsync(ct))
        {
            list.Add(new BoardMember
            {
                BoardId = r.GetGuid(0),
                UserId  = r.GetGuid(1),
                Role    = Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }
}