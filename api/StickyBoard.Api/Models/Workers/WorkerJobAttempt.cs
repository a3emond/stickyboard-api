using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.Workers;

[Table("worker_job_attempts")]
public class WorkerJobAttempt
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [ForeignKey("WorkerJob")]
    [Column("job_id")]
    public Guid JobId { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("ok")]
    public bool? Ok { get; set; }

    [Column("error")]
    public string? Error { get; set; }

    public WorkerJob? WorkerJob { get; set; }
}