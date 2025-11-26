using Npgsql;
using NpgsqlTypes;
using StickyBoard.Core.Common;
using StickyBoard.Core.Models.Automation.Jobs;

namespace StickyBoard.Core.Repositories.Automation.Jobs;

public sealed class WorkerJobRepository : IWorkerJobRepository
{
    private readonly NpgsqlDataSource _db;

    public WorkerJobRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    // INSERT (enqueue)
    // ------------------------------------------------------------
    public async Task<long> InsertAsync(WorkerJob job, CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO worker_jobs(kind, payload, status, priority, attempts, available_at)
                           VALUES (@kind, @payload, @status, @priority, @attempts, @available_at)
                           RETURNING id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("kind", job.Kind);
        cmd.Parameters.AddWithValue("status", job.Status);
        cmd.Parameters.AddWithValue("priority", job.Priority);
        cmd.Parameters.AddWithValue("attempts", job.Attempts);
        cmd.Parameters.AddWithValue("available_at", job.AvailableAt);

        var payloadParam = cmd.Parameters.Add("payload", NpgsqlDbType.Jsonb);
        payloadParam.Value = job.Payload.RootElement.GetRawText();

        var id = (long)(await cmd.ExecuteScalarAsync(ct))!;
        return id;
    }

    // ------------------------------------------------------------
    // LOCK + FETCH NEXT AVAILABLE (atomic dequeue)
    // ------------------------------------------------------------
    public async Task<WorkerJob?> LockNextAvailableAsync(CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET status = 'running',
                               updated_at = NOW()
                           WHERE id = (
                               SELECT id
                               FROM worker_jobs
                               WHERE status = 'queued'
                                 AND available_at <= NOW()
                               ORDER BY priority ASC, created_at ASC
                               LIMIT 1
                               FOR UPDATE SKIP LOCKED
                           )
                           RETURNING *;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        return MappingHelper.MapEntity<WorkerJob>(r);
    }

    // ------------------------------------------------------------
    // MARK RUNNING (idempotent – safe even after LockNextAvailable)
    // ------------------------------------------------------------
    public async Task MarkRunningAsync(long jobId, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET status = 'running',
                               updated_at = NOW()
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("id", jobId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------
    // MARK DONE
    // ------------------------------------------------------------
    public async Task MarkDoneAsync(long jobId, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET status = 'done',
                               updated_at = NOW(),
                               last_error = NULL
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("id", jobId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------
    // MARK FAILED (final, no retry)
    // ------------------------------------------------------------
    public async Task MarkFailedAsync(long jobId, string error, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET status = 'dead',
                               attempts = attempts + 1,
                               last_error = @error,
                               updated_at = NOW()
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", jobId);
        cmd.Parameters.AddWithValue("error", error);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------
    // INCREMENT ATTEMPTS (used before reschedule)
    // ------------------------------------------------------------
    public async Task IncrementAttemptsAsync(long jobId, string? error, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET attempts   = attempts + 1,
                               last_error = @error,
                               updated_at = NOW()
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", jobId);
        cmd.Parameters.AddWithValue("error", (object?)error ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------
    // RESCHEDULE (backoff - keep it queued)
    // ------------------------------------------------------------
    public async Task RescheduleAsync(long jobId, DateTime nextAttemptAt, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_jobs
                           SET status       = 'queued',
                               available_at = @available_at,
                               updated_at   = NOW()
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", jobId);
        cmd.Parameters.AddWithValue("available_at", nextAttemptAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
    {
        return _db.OpenConnectionAsync(ct);
    }
}