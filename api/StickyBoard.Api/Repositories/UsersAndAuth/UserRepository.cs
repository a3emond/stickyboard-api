using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth;

public sealed class UserRepository : RepositoryBase<User>
{
    public UserRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override User Map(NpgsqlDataReader reader)
        => MappingHelper.MapEntity<User>(reader);

    // ----------------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(User e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO users (email, display_name, avatar_uri, prefs)
            VALUES (@em, @dn, @av, @pf)
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("em", e.Email);
        cmd.Parameters.AddWithValue("dn", e.DisplayName);
        cmd.Parameters.AddWithValue("av", (object?)e.AvatarUri ?? DBNull.Value);
        cmd.Parameters.Add("pf", NpgsqlDbType.Jsonb)
           .Value = e.Prefs?.RootElement.GetRawText() ?? "{}";

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ----------------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(User e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE users SET
                display_name = @dn,
                avatar_uri = @av,
                prefs = @pf,
                updated_at = NOW()
            WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("dn", e.DisplayName);
        cmd.Parameters.AddWithValue("av", (object?)e.AvatarUri ?? DBNull.Value);
        cmd.Parameters.Add("pf", NpgsqlDbType.Jsonb)
            .Value = e.Prefs?.RootElement.GetRawText() ?? "{}";

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // Inherit soft-delete from base

    // ----------------------------------------------------------------------
    // QUERIES
    // ----------------------------------------------------------------------
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE LOWER(email) = LOWER(@em)
              AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("em", email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IEnumerable<User>> SearchByDisplayNameAsync(string partial, CancellationToken ct)
    {
        var list = new List<User>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE display_name ILIKE @pattern
              AND deleted_at IS NULL
            ORDER BY display_name ASC;", conn);

        cmd.Parameters.AddWithValue("pattern", $"%{partial}%");

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM users
            WHERE id = ANY(@ids)
              AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("ids", ids.ToArray());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return MapReaderToList(reader);
    }
}
