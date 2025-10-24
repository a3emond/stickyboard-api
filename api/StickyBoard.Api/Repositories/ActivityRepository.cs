using Npgsql;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class ActivityRepository : RepositoryBase<Activity>
    {
        public ActivityRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Activity Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Activity>(reader);

        // Insert new activity (append-only)
        public override async Task<Guid> CreateAsync(Activity e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO activities (board_id, card_id, actor_id, act_type, payload)
                VALUES (@board, @card, @actor, @type, @payload)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("card", (object?)e.CardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("actor", (object?)e.ActorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("type", e.ActType.ToString());
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // Append-only, no update logic
        public override Task<bool> UpdateAsync(Activity e, CancellationToken ct)
            => Task.FromResult(false);

        // Deletion discouraged but possible (for admin cleanup)
        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM activities WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL USEFUL QUERIES
        // ----------------------------------------------------------------------

        // Get all activities for a given board (ordered by time, most recent first)
        public async Task<IEnumerable<Activity>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Activity>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM activities
                WHERE board_id = @board
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("board", boardId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Get all activities related to a specific card
        public async Task<IEnumerable<Activity>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<Activity>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM activities
                WHERE card_id = @card
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("card", cardId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Optionally: retrieve the most recent N activities (for dashboard feeds)
        public async Task<IEnumerable<Activity>> GetRecentAsync(int limit, CancellationToken ct)
        {
            var list = new List<Activity>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM activities
                ORDER BY created_at DESC
                LIMIT @limit", conn);

            cmd.Parameters.AddWithValue("limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
