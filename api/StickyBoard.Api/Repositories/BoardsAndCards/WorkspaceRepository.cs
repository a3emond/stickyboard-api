using Npgsql;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class WorkspaceRepository : RepositoryBase<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(NpgsqlDataSource db) : base(db) { }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(Workspace entity, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO workspaces (id, name, created_by)
            VALUES (@id, @name, @created_by)
            RETURNING id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        var id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", entity.Name);
        cmd.Parameters.AddWithValue("created_by", entity.CreatedBy);

        return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
    }

    // ---------------------------------------------------------------------
    // UPDATE (concurrency-safe)
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(Workspace entity, CancellationToken ct)
    {
        var sql = $@"
            UPDATE workspaces
            SET name = @name
            WHERE {ConcurrencyWhere(entity)}
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", entity.Id);
        cmd.Parameters.AddWithValue("name", entity.Name);

        BindConcurrencyParameters(cmd, entity);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // ---------------------------------------------------------------------
    // GET WORKSPACES FOR USER
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var sql = ApplySoftDelete(@"
            SELECT w.*
            FROM workspaces w
            JOIN workspace_members wm ON wm.workspace_id = w.id
            WHERE wm.user_id = @user_id
            ORDER BY w.updated_at DESC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("user_id", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // ---------------------------------------------------------------------
    // MEMBERSHIP CHECK
    // ---------------------------------------------------------------------
    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct)
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
    // GET USER ROLE IN WORKSPACE
    // ---------------------------------------------------------------------
    public async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct)
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

        if (result is null)
            return null;

        return Enum.Parse<WorkspaceRole>(result.ToString()!, true);
    }


    // ---------------------------------------------------------------------
    // SEARCH BY NAME
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Workspace>> SearchByNameAsync(string query, CancellationToken ct)
    {
        var sql = ApplySoftDelete(@"
            SELECT *
            FROM workspaces
            WHERE name ILIKE @q
            ORDER BY updated_at DESC
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("q", $"%{query}%");

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // ---------------------------------------------------------------------
    // GET MEMBER IDS
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<Guid>> GetMemberIdsAsync(Guid workspaceId, CancellationToken ct)
    {
        const string sql = @"
            SELECT user_id
            FROM workspace_members
            WHERE workspace_id = @workspace_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("workspace_id", workspaceId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        var result = new List<Guid>();
        while (await r.ReadAsync(ct))
            result.Add(r.GetGuid(0));

        return result;
    }

    // ---------------------------------------------------------------------
    // SOFT DELETE + CASCADE (WORKER-BASED)
    // ---------------------------------------------------------------------
    public async Task<bool> SoftDeleteCascadeAsync(Guid workspaceId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE workspaces
            SET deleted_at = NOW()
            WHERE id = @id AND deleted_at IS NULL
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", workspaceId);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }
}
