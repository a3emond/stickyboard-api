using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.Workers;

[Table("worker_jobs")]
public class WorkerJob
{
    [Key] public Guid Id { get; set; }

    [Column("job_kind")]
    public JobKind JobKind { get; set; }

    public short Priority { get; set; } = 0;

    [Column("run_at")]
    public DateTime RunAt { get; set; } = DateTime.UtcNow;

    [Column("max_attempts")]
    public short MaxAttempts { get; set; } = 10;

    public int Attempt { get; set; } = 0;

    [Column("dedupe_key")]
    public string? DedupeKey { get; set; }

    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    public JobStatus Status { get; set; } = JobStatus.queued;

    [Column("claimed_by")]
    public string? ClaimedBy { get; set; }

    [Column("claimed_at")]
    public DateTime? ClaimedAt { get; set; }

    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}