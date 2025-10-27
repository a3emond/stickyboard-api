using Npgsql;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class OperationRepository : RepositoryBase<Operation>
    {
        public OperationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Operation Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Operation>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Operation e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO operations (
                    device_id, user_id, entity, entity_id, op_type, payload, version_prev, version_next
                )
                VALUES (@device, @user, @entity, @eid, @type, @payload, @vp, @vn)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("device", e.DeviceId);
            cmd.Parameters.AddWithValue("user", e.UserId);
            cmd.Parameters.AddWithValue("entity", e.Entity.ToString());
            cmd.Parameters.AddWithValue("eid", e.EntityId);
            cmd.Parameters.AddWithValue("type", e.OpType);
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("vp", (object?)e.VersionPrev ?? DBNull.Value);
            cmd.Parameters.AddWithValue("vn", (object?)e.VersionNext ?? DBNull.Value);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override Task<bool> UpdateAsync(Operation e, CancellationToken ct)
        {
            // Operations are immutable by design
            return Task.FromResult(false);
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            // Normally not deleted, but may be pruned by maintenance jobs
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM operations WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve all operations for a given user
        public async Task<IEnumerable<Operation>> GetByUserAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Operation>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM operations
                WHERE user_id = @uid
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all operations performed by a specific device (for device sync logs)
        public async Task<IEnumerable<Operation>> GetByDeviceAsync(Guid deviceId, CancellationToken ct)
        {
            var list = new List<Operation>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM operations
                WHERE device_id = @dev
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("dev", deviceId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all operations since a given timestamp (for incremental sync)
        public async Task<IEnumerable<Operation>> GetSinceAsync(DateTime sinceUtc, CancellationToken ct)
        {
            var list = new List<Operation>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM operations
                WHERE created_at > @since
                ORDER BY created_at ASC", conn);

            cmd.Parameters.AddWithValue("since", sinceUtc);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve pending (unprocessed) operations, used by background workers
        public async Task<IEnumerable<Operation>> GetPendingAsync(CancellationToken ct)
        {
            var list = new List<Operation>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM operations
                WHERE processed = FALSE
                ORDER BY created_at ASC", conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Mark an operation as processed (for worker cleanup)
        public async Task<bool> MarkProcessedAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE operations
                SET processed = TRUE,
                    processed_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // Delete operations older than a specified age (maintenance)
        public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM operations
                WHERE created_at < @cutoff", conn);

            cmd.Parameters.AddWithValue("cutoff", cutoffUtc);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
