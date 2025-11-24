using Npgsql;

namespace StickyBoard.Core.Repositories.Automation.Jobs;

public sealed class WorkerJobAttemptRepository : IWorkerJobAttemptRepository
{
    private readonly NpgsqlDataSource _db;

    public WorkerJobAttemptRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    // START ATTEMPT
    // ------------------------------------------------------------
    public async Task<long> StartAsync(long jobId, CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO worker_job_attempts(job_id, started_at)
                           VALUES (@job_id, NOW())
                           RETURNING id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("job_id", jobId);

        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }

    // ------------------------------------------------------------
    // FINISH ATTEMPT
    // ------------------------------------------------------------
    public async Task<bool> FinishAsync(long attemptId, string? error, CancellationToken ct)
    {
        const string sql = """
                           UPDATE worker_job_attempts
                           SET finished_at = NOW(),
                               error = @error
                           WHERE id = @id;
                           """;

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", attemptId);
        cmd.Parameters.AddWithValue("error", (object?)error ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
    {
        return _db.OpenConnectionAsync(ct);
    }
}