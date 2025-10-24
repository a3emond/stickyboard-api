using Npgsql;
using StickyBoard.Api.Models.Users;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class AuthUserRepository : RepositoryBase<AuthUser>
    {
        public AuthUserRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override AuthUser Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<AuthUser>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(AuthUser e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO auth_users (user_id, password_hash, role)
                VALUES (@uid, @hash, @role)
                RETURNING user_id", conn);

            cmd.Parameters.AddWithValue("uid", e.Id);
            cmd.Parameters.AddWithValue("hash", e.PasswordHash);
            cmd.Parameters.AddWithValue("role", e.Role);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(AuthUser e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE auth_users SET
                    password_hash=@hash,
                    role=@role,
                    updated_at=now()
                WHERE user_id=@uid", conn);

            cmd.Parameters.AddWithValue("uid", e.Id);
            cmd.Parameters.AddWithValue("hash", e.PasswordHash);
            cmd.Parameters.AddWithValue("role", e.Role);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM auth_users WHERE user_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve authentication record by user_id (used in AuthService)
        public async Task<AuthUser?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM auth_users
                WHERE user_id = @uid", conn);

            cmd.Parameters.AddWithValue("uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
        
        // Retrieve user by email joining AuthUsers - Users
        public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT au.*
                FROM auth_users au
                JOIN users u ON u.id = au.user_id
                WHERE LOWER(u.email) = LOWER(@em)", conn);

            cmd.Parameters.AddWithValue("em", email);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
    }
}
