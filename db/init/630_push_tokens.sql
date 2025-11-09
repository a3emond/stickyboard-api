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