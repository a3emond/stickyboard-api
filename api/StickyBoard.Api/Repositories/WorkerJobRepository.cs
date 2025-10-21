using Npgsql;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class WorkerJobRepository : RepositoryBase<WorkerJob>
    {
        public WorkerJobRepository(string connectionString) : base(connectionString) { }

        protected override WorkerJob Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<WorkerJob>(reader);

        public override async Task<Guid> CreateAsync(WorkerJob e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO worker_jobs (job_kind, priority, run_at, max_attempts, payload)
                VALUES (@kind, @prio, @run, @max, @payload)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("kind", e.JobKind.ToString());
            cmd.Parameters.AddWithValue("prio", e.Priority);
            cmd.Parameters.AddWithValue("run", e.RunAt);
            cmd.Parameters.AddWithValue("max", e.MaxAttempts);
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(WorkerJob e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE worker_jobs SET
                    status=@status,
                    attempt=@attempt,
                    claimed_by=@claimer,
                    claimed_at=@claimed_at,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("status", e.Status.ToString());
            cmd.Parameters.AddWithValue("attempt", e.Attempt);
            cmd.Parameters.AddWithValue("claimer", (object?)e.ClaimedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("claimed_at", (object?)e.ClaimedAt ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM worker_jobs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
