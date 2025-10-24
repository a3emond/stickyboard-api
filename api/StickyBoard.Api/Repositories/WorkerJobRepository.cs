using Npgsql;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class WorkerJobRepository : RepositoryBase<WorkerJob>
    {
        public WorkerJobRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override WorkerJob Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<WorkerJob>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(WorkerJob e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO worker_jobs (job_kind, priority, run_at, max_attempts, payload)
                VALUES (@kind, @prio, @run, @max, @payload)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("kind", e.JobKind.ToString());
            cmd.Parameters.AddWithValue("prio", e.Priority);
            cmd.Parameters.AddWithValue("run", e.RunAt);
            cmd.Parameters.AddWithValue("max", e.MaxAttempts);
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(WorkerJob e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE worker_jobs SET
                    status = @status,
                    attempt = @attempt,
                    claimed_by = @claimer,
                    claimed_at = @claimed_at,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("status", e.Status.ToString());
            cmd.Parameters.AddWithValue("attempt", e.Attempt);
            cmd.Parameters.AddWithValue("claimer", (object?)e.ClaimedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("claimed_at", (object?)e.ClaimedAt ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM worker_jobs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve jobs ready to run (unclaimed, scheduled for now or earlier)
        public async Task<IEnumerable<WorkerJob>> GetPendingAsync(CancellationToken ct)
        {
            var list = new List<WorkerJob>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM worker_jobs
                WHERE status = 'pending'
                  AND run_at <= now()
                ORDER BY priority DESC, run_at ASC
                LIMIT 100", conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Atomically claim a job for a worker instance
        public async Task<WorkerJob?> ClaimJobAsync(string workerId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            // Select the next pending job (for update)
            await using var selectCmd = new NpgsqlCommand(@"
                SELECT * FROM worker_jobs
                WHERE status = 'pending'
                  AND run_at <= now()
                ORDER BY priority DESC, run_at ASC
                LIMIT 1
                FOR UPDATE SKIP LOCKED", conn, tx);

            await using var reader = await selectCmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            var job = Map(reader);
            await reader.CloseAsync();

            // Mark as claimed
            await using var updateCmd = new NpgsqlCommand(@"
                UPDATE worker_jobs
                SET status = 'running',
                    claimed_by = @worker,
                    claimed_at = now(),
                    updated_at = now(),
                    attempt = attempt + 1
                WHERE id = @id", conn, tx);

            updateCmd.Parameters.AddWithValue("worker", workerId);
            updateCmd.Parameters.AddWithValue("id", job.Id);

            await updateCmd.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);

            job.Status = JobStatus.running;
            job.ClaimedBy = workerId;
            job.ClaimedAt = DateTime.UtcNow;
            job.Attempt++;

            return job;
        }

        // Mark job as successfully completed
        public async Task<bool> MarkCompletedAsync(Guid jobId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE worker_jobs
                SET status = 'completed',
                    completed_at = now(),
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", jobId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // Mark job as failed (with retry logic)
        public async Task<bool> MarkFailedAsync(Guid jobId, string? error, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE worker_jobs
                SET status = 'failed',
                    error_message = @err,
                    updated_at = now()
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("id", jobId);
            cmd.Parameters.AddWithValue("err", (object?)error ?? DBNull.Value);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // Delete all jobs older than a cutoff (cleanup)
        public async Task<int> DeleteExpiredAsync(DateTime cutoffUtc, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM worker_jobs
                WHERE created_at < @cutoff
                   OR (status IN ('completed', 'failed') AND completed_at < @cutoff)", conn);

            cmd.Parameters.AddWithValue("cutoff", cutoffUtc);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
