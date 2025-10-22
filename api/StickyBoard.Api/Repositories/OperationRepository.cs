using Npgsql;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class OperationRepository : RepositoryBase<Operation>
    {
        public OperationRepository(NpgsqlDataSource connectionString) : base(connectionString) { }

        protected override Operation Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Operation>(reader);

        public override async Task<Guid> CreateAsync(Operation e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO operations (device_id, user_id, entity, entity_id, op_type, payload, version_prev, version_next)
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

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Operation e)
        {
            // Operations are immutable (no updates)
            return false;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            // Normally not deleted, but may be pruned by maintenance jobs
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM operations WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
