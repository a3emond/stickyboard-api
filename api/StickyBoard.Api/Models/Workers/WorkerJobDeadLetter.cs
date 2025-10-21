using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.Workers;

[Table("worker_job_deadletters")]
public class WorkerJobDeadLetter
{
    [Key] public Guid Id { get; set; }

    [Column("job_id")]
    [ForeignKey("WorkerJob")]
    public Guid? JobId { get; set; }

    [Column("job_kind")]
    public JobKind JobKind { get; set; }

    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    public int Attempts { get; set; } = 0;

    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("dead_at")]
    public DateTime DeadAt { get; set; } = DateTime.UtcNow;

    public WorkerJob? WorkerJob { get; set; }
}