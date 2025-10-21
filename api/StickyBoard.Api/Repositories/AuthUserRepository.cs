using Npgsql;
using StickyBoard.Api.Models.Users;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class AuthUserRepository : RepositoryBase<AuthUser>
    {
        public AuthUserRepository(string connectionString) : base(connectionString) { }

        protected override AuthUser Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<AuthUser>(reader);

        public override async Task<Guid> CreateAsync(AuthUser e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO auth_users (user_id, password_hash, role)
                VALUES (@uid, @hash, @role)
                RETURNING user_id", conn);

            cmd.Parameters.AddWithValue("uid", e.Id);
            cmd.Parameters.AddWithValue("hash", e.PasswordHash);
            cmd.Parameters.AddWithValue("role", e.Role.ToString());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(AuthUser e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE auth_users SET
                    password_hash=@hash,
                    role=@role,
                    updated_at=now()
                WHERE user_id=@uid", conn);

            cmd.Parameters.AddWithValue("uid", e.Id);
            cmd.Parameters.AddWithValue("hash", e.PasswordHash);
            cmd.Parameters.AddWithValue("role", e.Role.ToString());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM auth_users WHERE user_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}