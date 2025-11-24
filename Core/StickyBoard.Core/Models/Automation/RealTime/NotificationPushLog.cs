using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Core.Models.Automation.RealTime;

[Table("notification_push_log")]
public class NotificationPushLog
{
    [Column("notification_id")] public Guid NotificationId { get; set; }

    [Column("push_token_id")] public long PushTokenId { get; set; }

    [Column("provider")] public PushProvider Provider { get; set; }

    [Column("status")] public WorkerJobStatus Status { get; set; }

    [Column("last_error")] public string? LastError { get; set; }

    [Column("first_attempt_at")] public DateTime FirstAttemptAt { get; set; }

    [Column("last_attempt_at")] public DateTime LastAttemptAt { get; set; }
}
/*
CREATE TABLE IF NOT EXISTS notification_push_log (
  notification_id uuid          NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
  push_token_id   bigint        NOT NULL REFERENCES push_tokens(id)   ON DELETE CASCADE,
  provider        push_provider NOT NULL,
  status          worker_job_status NOT NULL DEFAULT 'queued',  -- reuse enum
  last_error      text,
  first_attempt_at timestamptz  NOT NULL DEFAULT now(),
  last_attempt_at  timestamptz  NOT NULL DEFAULT now(),

  PRIMARY KEY (notification_id, push_token_id)
);
*/