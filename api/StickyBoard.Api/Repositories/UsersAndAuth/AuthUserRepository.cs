using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth;

public sealed class AuthUserRepository : RepositoryBase<AuthUser>
{
    public AuthUserRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override AuthUser Map(NpgsqlDataReader reader)
        => MappingHelper.MapEntity<AuthUser>(reader);

    // ----------------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------------
    public override async Task<Guid> CreateAsync(AuthUser e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO auth_users (user_id, password_hash, role)
            VALUES (@uid, @hash, @role)
            RETURNING user_id;", conn);

        cmd.Parameters.AddWithValue("uid", e.UserId);
        cmd.Parameters.AddWithValue("hash", e.PasswordHash);
        cmd.Parameters.AddWithValue("role", e.Role);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ----------------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(AuthUser e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE auth_users SET
                password_hash = @hash,
                role = @role,
                updated_at = NOW()
            WHERE user_id = @uid;", conn);

        cmd.Parameters.AddWithValue("uid", e.UserId);
        cmd.Parameters.AddWithValue("hash", e.PasswordHash);
        cmd.Parameters.AddWithValue("role", e.Role);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ----------------------------------------------------------------------
    // Soft delete inherited — DO NOT override DeleteAsync
    // ----------------------------------------------------------------------

    // ----------------------------------------------------------------------
    // QUERIES
    // ----------------------------------------------------------------------
    public async Task<AuthUser?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM auth_users
            WHERE user_id = @uid AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("uid", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT au.*
            FROM auth_users au
            JOIN users u ON u.id = au.user_id
            WHERE LOWER(u.email) = LOWER(@em)
              AND u.deleted_at IS NULL 
              AND au.deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("em", email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE auth_users
            SET last_login = NOW(), updated_at = NOW()
            WHERE user_id = @uid;", conn);

        cmd.Parameters.AddWithValue("uid", userId);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }
}
