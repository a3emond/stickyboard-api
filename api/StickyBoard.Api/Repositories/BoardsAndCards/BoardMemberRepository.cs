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
    // ADD
    // ---------------------------------------------------------------------
    public async Task<bool> AddAsync(BoardMember entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO board_members (board_id, user_id, role)
            VALUES (@board_id, @user_id, @role)
            ON CONFLICT (board_id, user_id) DO NOTHING
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", entity.BoardId);
        cmd.Parameters.AddWithValue("user_id", entity.UserId);
        cmd.Parameters.AddWithValue("role", (object?)entity.Role?.ToString() ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // REMOVE
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
    // EXISTS
    // ---------------------------------------------------------------------
    public async Task<bool> ExistsAsync(Guid boardId, Guid userId, CancellationToken ct)
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
    // GET ROLE
    // ---------------------------------------------------------------------
    public async Task<WorkspaceRole?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT role
            FROM board_members
            WHERE board_id = @board_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        var result = await cmd.ExecuteScalarAsync(ct);

        return result is null
            ? null
            : Enum.Parse<WorkspaceRole>(result.ToString()!, true);
    }
    
    // ---------------------------------------------------------------------
    // UPDATE ROLE
    // ---------------------------------------------------------------------
    public async Task<bool> UpdateRoleAsync(Guid boardId, Guid userId, WorkspaceRole role, CancellationToken ct)
    {
        const string sql = @"
        UPDATE board_members
        SET role = @role
        WHERE board_id = @board_id
          AND user_id = @user_id
    ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("board_id", boardId);
        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("role", role.ToString());

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }


    // ---------------------------------------------------------------------
    // GET BY BOARD
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetByBoardAsync(Guid boardId, CancellationToken ct)
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
                UserId = r.GetGuid(1),
                Role = r.IsDBNull(2)
                    ? null
                    : Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }

    // ---------------------------------------------------------------------
    // GET BY USER
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<BoardMember>> GetByUserAsync(Guid userId, CancellationToken ct)
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
                UserId = r.GetGuid(1),
                Role = r.IsDBNull(2)
                    ? null
                    : Enum.Parse<WorkspaceRole>(r.GetString(2), true)
            });
        }

        return list;
    }
}
