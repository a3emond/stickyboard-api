using Npgsql;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Social;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class UserRelationRepository : RepositoryBase<UserRelation>
    {
        public UserRelationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override UserRelation Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<UserRelation>(r);

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public override async Task<Guid> CreateAsync(UserRelation e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO user_relations (user_id, friend_id, status)
                VALUES (@u, @f, @s)
                ON CONFLICT (user_id, friend_id) DO UPDATE
                    SET status = EXCLUDED.status,
                        updated_at = now()
                RETURNING user_id;", conn);

            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("f", e.FriendId);
            cmd.Parameters.AddWithValue("s", e.Status.ToString().ToLower());
            await cmd.ExecuteScalarAsync(ct);

            return e.UserId;
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        public override async Task<bool> UpdateAsync(UserRelation e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE user_relations
                SET status = @s,
                    updated_at = now()
                WHERE user_id = @u AND friend_id = @f;", conn);

            cmd.Parameters.AddWithValue("s", e.Status.ToString().ToLower());
            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("f", e.FriendId);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // SOFT DELETE (set inactive)
        // ------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE user_relations
                SET status = 'inactive',
                    updated_at = now()
                WHERE user_id = @id;", conn);

            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // DELETE PAIR (remove both directions)
        // ------------------------------------------------------------
        public async Task<bool> DeletePairAsync(Guid a, Guid b, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM user_relations
                WHERE (user_id=@a AND friend_id=@b)
                   OR (user_id=@b AND friend_id=@a);", conn);

            cmd.Parameters.AddWithValue("a", a);
            cmd.Parameters.AddWithValue("b", b);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // GET FRIENDS LIST
        // ------------------------------------------------------------
        public async Task<IEnumerable<UserRelation>> GetFriendsAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<UserRelation>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM user_relations
                WHERE user_id = @u AND status = 'active'
                ORDER BY created_at DESC;", conn);

            cmd.Parameters.AddWithValue("u", userId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // GET SPECIFIC RELATION
        // ------------------------------------------------------------
        public async Task<UserRelation?> GetAsync(Guid userId, Guid friendId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM user_relations
                WHERE user_id=@u AND friend_id=@f;", conn);

            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("f", friendId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
    }
}
