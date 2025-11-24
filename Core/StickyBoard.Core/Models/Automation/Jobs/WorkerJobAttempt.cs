using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Core.Models.Base;

namespace StickyBoard.Core.Models.Automation.Jobs;

[Table("worker_job_attempts")]
public class WorkerJobAttempt : IEntity
{
    [Column("id")] public long Id { get; set; }

    [Column("job_id")] public long JobId { get; set; }

    [Column("started_at")] public DateTime StartedAt { get; set; }

    [Column("finished_at")] public DateTime? FinishedAt { get; set; }

    [Column("error")] public string? Error { get; set; }
}

/*
CREATE TABLE IF NOT EXISTS worker_job_attempts (
  id          bigserial PRIMARY KEY,
  job_id      bigint NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
  started_at  timestamptz NOT NULL,
  finished_at timestamptz,
  error       text
);
*/