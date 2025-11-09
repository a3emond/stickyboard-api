CREATE TABLE IF NOT EXISTS sync_cursors (
  user_id     uuid NOT NULL,
  scope_type  text NOT NULL CHECK (scope_type IN ('workspace','board','inbox')),
  scope_id    uuid,
  last_cursor bigint NOT NULL DEFAULT 0,
  updated_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(user_id, scope_type, scope_id)
);
CREATE INDEX IF NOT EXISTS ix_sync_cursors_scope ON sync_cursors(scope_type, scope_id);
