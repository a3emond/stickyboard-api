using Npgsql;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class WorkerJobDeadletterRepository : RepositoryBase<WorkerJobDeadletter>
    {
        public WorkerJobDeadletterRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override WorkerJobDeadletter Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<WorkerJobDeadletter>(r);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(WorkerJobDeadletter e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO worker_job_deadletters (job_id, job_kind, payload, attempts, last_error)
                VALUES (@jid, @kind, @payload, @a, @err)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("jid", (object?)e.JobId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("kind", e.JobKind.ToString());
            cmd.Parameters.AddWithValue("payload", e.Payload.RootElement.GetRawText());
            cmd.Parameters.AddWithValue("a", e.Attempts);
            cmd.Parameters.AddWithValue("err", (object?)e.LastError ?? DBNull.Value);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override Task<bool> UpdateAsync(WorkerJobDeadletter e, CancellationToken ct)
        {
            // Deadletters are immutable logs
            return Task.FromResult(false);
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM worker_job_deadletters WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // QUERIES
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<WorkerJobDeadletter>> GetAllAsync(CancellationToken ct)
        {
            var list = new List<WorkerJobDeadletter>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM worker_job_deadletters ORDER BY dead_at DESC", conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        public async Task<IEnumerable<WorkerJobDeadletter>> GetByKindAsync(string jobKind, CancellationToken ct)
        {
            var list = new List<WorkerJobDeadletter>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM worker_job_deadletters WHERE job_kind=@k ORDER BY dead_at DESC", conn);

            cmd.Parameters.AddWithValue("k", jobKind);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
