using Npgsql;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class ActivityRepository : RepositoryBase<Activity>
    {
        public ActivityRepository(NpgsqlDataSource connectionString) : base(connectionString) { }

        protected override Activity Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Activity>(reader);

        public override async Task<Guid> CreateAsync(Activity e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO activities (board_id, card_id, actor_id, act_type, payload)
                VALUES (@board, @card, @actor, @type, @payload)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("card", (object?)e.CardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("actor", (object?)e.ActorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("type", e.ActType.ToString());
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Activity e)
        {
            // Activities are append-only (no update logic)
            return false;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            // Activities should normally not be deleted, but kept for audit
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM activities WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}