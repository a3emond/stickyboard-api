-- Push tokens for notifications
CREATE TABLE IF NOT EXISTS push_tokens (
  id         bigserial PRIMARY KEY,
  user_id    uuid NOT NULL,
  provider   text NOT NULL CHECK (provider IN ('fcm','apns','webpush')),
  token      text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(provider, token)
);
CREATE INDEX IF NOT EXISTS ix_push_user ON push_tokens(user_id);

-- =====================================================================
-- NOTIFICATION PUSH DELIVERY LOG (IDEMPOTENCY FOR FAN-OUT) (workers-related)
-- =====================================================================
CREATE TABLE IF NOT EXISTS notification_push_log (
  notification_id uuid        NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
  push_token_id   bigint      NOT NULL REFERENCES push_tokens(id)   ON DELETE CASCADE,
  provider        text        NOT NULL CHECK (provider IN ('fcm','apns','webpush')),
  status          text        NOT NULL CHECK (status IN ('pending','sent','failed')),
  last_error      text,
  first_attempt_at timestamptz NOT NULL DEFAULT now(),
  last_attempt_at  timestamptz NOT NULL DEFAULT now(),

  PRIMARY KEY (notification_id, push_token_id)
);

CREATE INDEX IF NOT EXISTS ix_notif_push_status
  ON notification_push_log(status, last_attempt_at);
