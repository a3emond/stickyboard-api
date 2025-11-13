using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.UsersAndAuth.Contracts;

namespace StickyBoard.Api.Repositories.UsersAndAuth;

/*
    Inherits RepositoryBase<T>:

    Provides:
    - Table name resolution via [Table] or type name.
    - Connection helpers (NpgsqlDataSource).
    - Soft-delete filtering when T : ISoftDeletable.
    - Standard reads:
        GetByIdAsync, GetAllAsync, ExistsAsync, CountAsync.
    - Paging:
        GetPagedAsync(limit, offset) ordered by updated_at.
    - Sync support:
        GetUpdatedSinceAsync and paged variant.
    - DeleteAsync:
        Soft delete if supported, otherwise hard delete.

    Child class must implement:
        Map(), CreateAsync(), UpdateAsync().
*/


public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override User Map(NpgsqlDataReader reader)
        => MappingHelper.MapEntity<User>(reader);

    // ============================================================
    // CREATE / UPDATE
    // ============================================================
    public override async Task<Guid> CreateAsync(User e, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO users (email, display_name, avatar_uri, prefs, groups)
            VALUES (@em, @dn, @av, @pf, @gr)
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("em", e.Email);
        cmd.Parameters.AddWithValue("dn", e.DisplayName);
        cmd.Parameters.AddWithValue("av", (object?)e.AvatarUri ?? DBNull.Value);
        cmd.Parameters.Add("pf", NpgsqlDbType.Jsonb).Value = e.Prefs?.RootElement.GetRawText() ?? "{}";
        cmd.Parameters.AddWithValue("gr", e.Groups ?? Array.Empty<string>());

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(User e, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users SET
                display_name = @dn,
                avatar_uri   = @av,
                prefs        = @pf,
                groups       = @gr,
                updated_at   = NOW()
            WHERE id = @id AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("dn", e.DisplayName);
        cmd.Parameters.AddWithValue("av", (object?)e.AvatarUri ?? DBNull.Value);
        cmd.Parameters.Add("pf", NpgsqlDbType.Jsonb).Value = e.Prefs?.RootElement.GetRawText() ?? "{}";
        cmd.Parameters.AddWithValue("gr", e.Groups ?? Array.Empty<string>());

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ============================================================
    // QUERIES
    // ============================================================
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE LOWER(email) = LOWER(@em) AND deleted_at IS NULL
            LIMIT 1;", conn);
        cmd.Parameters.AddWithValue("em", email);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT 1 FROM users
            WHERE LOWER(email) = LOWER(@em) AND deleted_at IS NULL;", conn);
        cmd.Parameters.AddWithValue("em", email);
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<IEnumerable<User>> SearchByDisplayNameAsync(string partial, CancellationToken ct)
    {
        var list = new List<User>();
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE display_name ILIKE @pattern
              AND deleted_at IS NULL
            ORDER BY display_name ASC;", conn);

        cmd.Parameters.AddWithValue("pattern", $"%{partial}%");

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(Map(r));
        return list;
    }

    public async Task<PagedResult<User>> SearchByDisplayNamePagedAsync(string partial, int limit, int offset, CancellationToken ct)
    {
        await using var conn = await Conn(ct);

        // count
        await using var countCmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM users
            WHERE display_name ILIKE @pattern AND deleted_at IS NULL;", conn);
        countCmd.Parameters.AddWithValue("pattern", $"%{partial}%");
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        // page
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE display_name ILIKE @pattern AND deleted_at IS NULL
            ORDER BY display_name ASC
            LIMIT @limit OFFSET @offset;", conn);
        cmd.Parameters.AddWithValue("pattern", $"%{partial}%");
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var items = new List<User>();
        while (await r.ReadAsync(ct)) items.Add(Map(r));

        return PagedResult<User>.Create(items, total, limit, offset);
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idArray = ids as Guid[] ?? ids.ToArray();
        if (idArray.Length == 0) return Array.Empty<User>();

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE id = ANY(@ids) AND deleted_at IS NULL;", conn);

        cmd.Parameters.Add("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).Value = idArray;

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<IEnumerable<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken ct)
    {
        var arr = emails?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? Array.Empty<string>();
        if (arr.Length == 0) return Array.Empty<User>();

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE LOWER(email) = ANY(@ems) AND deleted_at IS NULL;", conn);

        cmd.Parameters.Add("ems", NpgsqlDbType.Array | NpgsqlDbType.Text).Value =
            arr.Select(s => s.ToLowerInvariant()).ToArray();

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // ============================================================
    // GROUPS
    // ============================================================
    public async Task<string[]> GetAllUserGroupsAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT groups FROM users
            WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string[] ?? Array.Empty<string>();
    }

    public async Task<bool> SetUserGroupsAsync(Guid userId, string[] groups, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
               SET groups = @gr, updated_at = NOW()
             WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("gr", groups ?? Array.Empty<string>());
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // Add (dedup): array_cat + SELECT DISTINCT over unnest
    public async Task<int> AddGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct)
    {
        var arr = (groups ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (arr.Length == 0) return 0;

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
               SET groups = ARRAY(
                   SELECT DISTINCT g FROM unnest(COALESCE(groups, ARRAY[]::text[])) g
                   UNION
                   SELECT DISTINCT x FROM unnest(@new) x
               ),
                   updated_at = NOW()
             WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.Add("new", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = arr;
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    // Remove many
    public async Task<int> RemoveGroupsAsync(Guid userId, IEnumerable<string> groups, CancellationToken ct)
    {
        var arr = (groups ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (arr.Length == 0) return 0;

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
               SET groups = ARRAY(
                    SELECT g FROM unnest(COALESCE(groups, ARRAY[]::text[])) g
                    WHERE NOT (g = ANY(@rm))
               ),
                   updated_at = NOW()
             WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.Add("rm", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = arr;
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> RenameGroupAsync(Guid userId, string oldName, string newName, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
               SET groups = ARRAY(
                    SELECT DISTINCT (CASE WHEN g = @old THEN @new ELSE g END)
                    FROM unnest(COALESCE(groups, ARRAY[]::text[])) AS g
               ),
                   updated_at = NOW()
             WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("old", oldName);
        cmd.Parameters.AddWithValue("new", newName);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> RemoveGroupAsync(Guid userId, string group, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users
               SET groups = ARRAY(
                    SELECT g FROM unnest(COALESCE(groups, ARRAY[]::text[])) AS g
                    WHERE g <> @grp
               ),
                   updated_at = NOW()
             WHERE id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("grp", group);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IEnumerable<User>> GetUsersByGroupAsync(string group, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE @grp = ANY(groups) AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("grp", group);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<string[]> SearchGroupsAsync(Guid userId, string partial, CancellationToken ct)
    {
        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT g
              FROM users, unnest(COALESCE(groups, ARRAY[]::text[])) AS g
             WHERE id = @uid AND g ILIKE @pattern;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("pattern", $"%{partial}%");

        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(r.GetString(0));
        return list.ToArray();
    }
}
