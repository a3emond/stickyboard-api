using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Worker
{
    [Table("worker_jobs")]
    public class WorkerJob : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("job_kind")] public JobKind JobKind { get; set; }
        [Column("priority")] public short Priority { get; set; }
        [Column("run_at")] public DateTime RunAt { get; set; }
        [Column("max_attempts")] public short MaxAttempts { get; set; }
        [Column("attempt")] public int Attempt { get; set; }
        [Column("dedupe_key")] public string? DedupeKey { get; set; }
        [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
        [Column("status")] public JobStatus Status { get; set; } = JobStatus.queued;
        [Column("claimed_by")] public string? ClaimedBy { get; set; }
        [Column("claimed_at")] public DateTime? ClaimedAt { get; set; }
        [Column("last_error")] public string? LastError { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Table("worker_job_attempts")]
    public class WorkerJobAttempt : IEntity
    {
        [Key, Column("id")] public long Id { get; set; }
        [Column("job_id")] public Guid JobId { get; set; }
        [Column("started_at")] public DateTime StartedAt { get; set; }
        [Column("finished_at")] public DateTime? FinishedAt { get; set; }
        [Column("ok")] public bool? Ok { get; set; }
        [Column("error")] public string? Error { get; set; }
    }

    [Table("worker_job_deadletters")]
    public class WorkerJobDeadletter : IEntity
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("job_id")] public Guid? JobId { get; set; }
        [Column("job_kind")] public JobKind JobKind { get; set; }
        [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
        [Column("attempts")] public int Attempts { get; set; }
        [Column("last_error")] public string? LastError { get; set; }
        [Column("dead_at")] public DateTime DeadAt { get; set; }
    }
}
