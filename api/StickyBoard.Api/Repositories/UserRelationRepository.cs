using Npgsql;
using StickyBoard.Api.Models.Social;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class UserRelationRepository : RepositoryBase<UserRelation>
    {
        public UserRelationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override UserRelation Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<UserRelation>(r);

        public override async Task<Guid> CreateAsync(UserRelation e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO user_relations (user_id, friend_id, status)
                VALUES (@u, @f, @s)
                RETURNING user_id", conn);

            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("f", e.FriendId);
            cmd.Parameters.AddWithValue("s", e.Status);
            await cmd.ExecuteScalarAsync(ct);
            return e.UserId;
        }

        public override async Task<bool> UpdateAsync(UserRelation e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE user_relations
                SET status=@s, accepted_at=CASE WHEN @s='accepted' THEN now() ELSE accepted_at END
                WHERE user_id=@u AND friend_id=@f", conn);

            cmd.Parameters.AddWithValue("s", e.Status);
            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("f", e.FriendId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM user_relations WHERE user_id=@id OR friend_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }
    }
}
