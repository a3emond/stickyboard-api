-- Realtime websocket session tracking (kept for future-proofing; Firebase is primary bus)
CREATE TABLE IF NOT EXISTS ws_sessions (
  id           bigserial PRIMARY KEY,
  user_id      uuid NOT NULL,
  node_id      text NOT NULL,
  connected_at timestamptz NOT NULL DEFAULT now(),
  last_seen_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ws_user ON ws_sessions(user_id);