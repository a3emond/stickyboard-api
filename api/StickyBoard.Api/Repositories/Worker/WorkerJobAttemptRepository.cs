using Npgsql;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class WorkerJobAttemptRepository : RepositoryBase<WorkerJobAttempt>
    {
        public WorkerJobAttemptRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override WorkerJobAttempt Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<WorkerJobAttempt>(r);

        // ----------------------------------------------------------------------
        // CREATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(WorkerJobAttempt e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO worker_job_attempts (job_id, started_at, finished_at, ok, error)
                VALUES (@j, @s, @f, @ok, @err)
                RETURNING job_id", conn);

            cmd.Parameters.AddWithValue("j", e.JobId);
            cmd.Parameters.AddWithValue("s", e.StartedAt);
            cmd.Parameters.AddWithValue("f", (object?)e.FinishedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ok", e.Ok);
            cmd.Parameters.AddWithValue("err", (object?)e.Error ?? DBNull.Value);

            await cmd.ExecuteScalarAsync(ct);
            return e.JobId;
        }

        public override Task<bool> UpdateAsync(WorkerJobAttempt e, CancellationToken ct)
        {
            // Attempts are immutable (append-only)
            return Task.FromResult(false);
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM worker_job_attempts WHERE job_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // QUERIES
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<WorkerJobAttempt>> GetByJobAsync(Guid jobId, CancellationToken ct)
        {
            var list = new List<WorkerJobAttempt>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM worker_job_attempts WHERE job_id=@j ORDER BY started_at DESC", conn);

            cmd.Parameters.AddWithValue("j", jobId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
