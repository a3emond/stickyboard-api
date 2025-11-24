using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Core.Models.Base;

namespace StickyBoard.Core.Models.Automation.Jobs;

[Table("worker_jobs")]
public class WorkerJob : IEntityUpdatable
{
    [Column("id")] public long Id { get; set; }

    [Column("kind")] public WorkerJobKind Kind { get; set; }

    [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    [Column("status")] public WorkerJobStatus Status { get; set; }

    [Column("priority")] public int Priority { get; set; }

    [Column("attempts")] public int Attempts { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("available_at")] public DateTime AvailableAt { get; set; }

    [Column("last_error")] public string? LastError { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
/*
CREATE TABLE IF NOT EXISTS worker_jobs (
  id           bigserial PRIMARY KEY,
  kind         worker_job_kind   NOT NULL,
  payload      jsonb             NOT NULL,
  status       worker_job_status NOT NULL DEFAULT 'queued',
  priority     int               NOT NULL DEFAULT 5,
  attempts     int               NOT NULL DEFAULT 0,
  created_at   timestamptz       NOT NULL DEFAULT now(),
  updated_at   timestamptz       NOT NULL DEFAULT now(),
  available_at timestamptz       NOT NULL DEFAULT now(),
  last_error   text
);
*/