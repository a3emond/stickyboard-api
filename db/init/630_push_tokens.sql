--------------------------------------------------
-- Push tokens for notifications
--------------------------------------------------
CREATE TABLE IF NOT EXISTS push_tokens (
  id         bigserial PRIMARY KEY,
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  provider   push_provider NOT NULL,
  token      text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(provider, token)
);

CREATE INDEX IF NOT EXISTS ix_push_user
  ON push_tokens(user_id);


--------------------------------------------------
-- NOTIFICATION PUSH DELIVERY LOG
-- (idempotency tracking per push_token per notification)
--------------------------------------------------
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

CREATE INDEX IF NOT EXISTS ix_notif_push_status
  ON notification_push_log(status, last_attempt_at);
