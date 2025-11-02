using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public class BoardFolderRepository : RepositoryBase<BoardFolder>
{
    public BoardFolderRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override BoardFolder Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<BoardFolder>(r);

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public override async Task<Guid> CreateAsync(BoardFolder e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO board_folders (
                org_id, user_id, name, icon, color, meta
            )
            VALUES (
                @org, @usr, @name, @icon, @color, @meta
            )
            RETURNING id", conn);

        cmd.Parameters.AddWithValue("org", (object?)e.OrgId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("usr", (object?)e.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("name", e.Name);
        cmd.Parameters.AddWithValue("icon", (object?)e.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("color", (object?)e.Color ?? DBNull.Value);

        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    public override async Task<bool> UpdateAsync(BoardFolder e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE board_folders
            SET name=@name,
                icon=@icon,
                color=@color,
                meta=@meta,
                updated_at=now()
            WHERE id=@id", conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("name", e.Name);
        cmd.Parameters.AddWithValue("icon", (object?)e.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("color", (object?)e.Color ?? DBNull.Value);

        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ------------------------------------------------------------
    // SELECT — By Org
    // ------------------------------------------------------------
    public async Task<IEnumerable<BoardFolder>> GetByOrgAsync(Guid orgId, CancellationToken ct)
    {
        var list = new List<BoardFolder>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM board_folders
            WHERE org_id=@org AND deleted_at IS NULL
            ORDER BY name ASC", conn);

        cmd.Parameters.AddWithValue("org", orgId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // SELECT — By User
    // ------------------------------------------------------------
    public async Task<IEnumerable<BoardFolder>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        var list = new List<BoardFolder>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM board_folders
            WHERE user_id=@usr AND deleted_at IS NULL
            ORDER BY name ASC", conn);

        cmd.Parameters.AddWithValue("usr", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // Get all folders visible to a user
    // personal + org folders (if member)
    // ------------------------------------------------------------
    public async Task<IEnumerable<BoardFolder>> GetAccessibleFoldersAsync(Guid userId, CancellationToken ct)
    {
        var list = new List<BoardFolder>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT bf.*
            FROM board_folders bf
            LEFT JOIN organization_members om
                ON bf.org_id = om.org_id AND om.user_id=@usr
            WHERE bf.deleted_at IS NULL
              AND (bf.user_id=@usr OR om.user_id IS NOT NULL)
            ORDER BY bf.name ASC", conn);

        cmd.Parameters.AddWithValue("usr", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }
}
