using Npgsql;
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


public sealed class AuthUserRepository : RepositoryBase<AuthUser>, IAuthUserRepository
{
    public AuthUserRepository(NpgsqlDataSource db) : base(db) { }

    protected override AuthUser Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<AuthUser>(r);

    // ============================================================
    // CREATE
    // ============================================================
    public override async Task<Guid> CreateAsync(AuthUser e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO auth_users (user_id, password_hash, role)
            VALUES (@uid, @hash, @role)
            RETURNING user_id;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("uid", e.UserId);
        cmd.Parameters.AddWithValue("hash", e.PasswordHash);
        cmd.Parameters.AddWithValue("role", e.Role);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ============================================================
    // UPDATE
    // ============================================================
    public override async Task<bool> UpdateAsync(AuthUser e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE auth_users
            SET password_hash = @hash,
                role = @role,
                updated_at = NOW()
            WHERE user_id = @uid
              AND deleted_at IS NULL;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("uid", e.UserId);
        cmd.Parameters.AddWithValue("hash", e.PasswordHash);
        cmd.Parameters.AddWithValue("role", e.Role);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ============================================================
    // QUERIES
    // ============================================================
    public async Task<AuthUser?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var sql = WithSoftDelete($@"
            SELECT * FROM auth_users
            WHERE user_id = @uid
            LIMIT 1;
        ");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"
            SELECT au.*
            FROM auth_users au
            JOIN users u ON u.id = au.user_id
            WHERE LOWER(u.email) = LOWER(@em)
              AND u.deleted_at IS NULL
              AND au.deleted_at IS NULL
            LIMIT 1;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("em", email);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    public async Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE auth_users
            SET last_login = NOW(), updated_at = NOW()
            WHERE user_id = @uid
              AND deleted_at IS NULL;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("uid", userId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }
}
