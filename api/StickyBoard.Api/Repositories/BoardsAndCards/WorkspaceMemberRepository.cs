using Npgsql;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly NpgsqlDataSource _db;

    public WorkspaceMemberRepository(NpgsqlDataSource db) => _db = db;

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
        => _db.OpenConnectionAsync(ct);

    // ---------------------------------------------------------------------
    // ADD
    // ---------------------------------------------------------------------
    public async Task<bool> AddAsync(WorkspaceMember entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO workspace_members (workspace_id, user_id, role)
            VALUES (@workspace_id, @user_id, @role)
            ON CONFLICT (workspace_id, user_id) DO NOTHING
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", entity.WorkspaceId);
        cmd.Parameters.AddWithValue("user_id", entity.UserId);
        cmd.Parameters.AddWithValue("role", entity.Role.ToString());

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // UPDATE ROLE
    // ---------------------------------------------------------------------
    public async Task<bool> UpdateRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct)
    {
        const string sql = @"
            UPDATE workspace_members
            SET role = @role
            WHERE workspace_id = @workspace_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);
        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("role", role.ToString());

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------------------
    public async Task<bool> RemoveAsync(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM workspace_members
            WHERE workspace_id = @workspace_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);
        cmd.Parameters.AddWithValue("user_id", userId);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // EXISTS
    // ---------------------------------------------------------------------
    public async Task<bool> ExistsAsync(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM workspace_members
            WHERE workspace_id = @workspace_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);
        cmd.Parameters.AddWithValue("user_id", userId);

        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    // ---------------------------------------------------------------------
    // GET ROLE
    // ---------------------------------------------------------------------
    public async Task<WorkspaceRole?> GetRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT role
            FROM workspace_members
            WHERE workspace_id = @workspace_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);
        cmd.Parameters.AddWithValue("user_id", userId);

        var result = await cmd.ExecuteScalarAsync(ct);

        return result is null
            ? null
            : Enum.Parse<WorkspaceRole>(result.ToString()!, true);
    }

    // ---------------------------------------------------------------------
    // GET BY WORKSPACE
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<WorkspaceMember>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        const string sql = @"
            SELECT workspace_id, user_id, role, joined_at
            FROM workspace_members
            WHERE workspace_id = @workspace_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var result = new List<WorkspaceMember>();
        while (await r.ReadAsync(ct))
        {
            result.Add(new WorkspaceMember
            {
                WorkspaceId = r.GetGuid(0),
                UserId = r.GetGuid(1),
                Role = Enum.Parse<WorkspaceRole>(r.GetString(2), true),
                JoinedAt = r.GetDateTime(3)
            });
        }

        return result;
    }

    // ---------------------------------------------------------------------
    // GET BY USER
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<WorkspaceMember>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT workspace_id, user_id, role, joined_at
            FROM workspace_members
            WHERE user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("user_id", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var result = new List<WorkspaceMember>();
        while (await r.ReadAsync(ct))
        {
            result.Add(new WorkspaceMember
            {
                WorkspaceId = r.GetGuid(0),
                UserId = r.GetGuid(1),
                Role = Enum.Parse<WorkspaceRole>(r.GetString(2), true),
                JoinedAt = r.GetDateTime(3)
            });
        }

        return result;
    }
}
