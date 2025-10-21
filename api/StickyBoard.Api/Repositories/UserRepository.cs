using Npgsql;
using StickyBoard.Api.Models.Users;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class UserRepository : RepositoryBase<User>
    {
        public UserRepository(string connectionString) : base(connectionString) { }

        protected override User Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<User>(reader);

        public override async Task<Guid> CreateAsync(User e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO users (email, display_name, avatar_uri, prefs)
                VALUES (@email, @display, @avatar, @prefs)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("email", e.Email);
            cmd.Parameters.AddWithValue("display", e.DisplayName);
            cmd.Parameters.AddWithValue("avatar", (object?)e.AvatarUri ?? DBNull.Value);
            cmd.Parameters.AddWithValue("prefs", e.Prefs.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(User e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE users SET
                    display_name=@display,
                    avatar_uri=@avatar,
                    prefs=@prefs,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("display", e.DisplayName);
            cmd.Parameters.AddWithValue("avatar", (object?)e.AvatarUri ?? DBNull.Value);
            cmd.Parameters.AddWithValue("prefs", e.Prefs.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM users WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
