CREATE TABLE IF NOT EXISTS event_outbox (
  cursor       BIGSERIAL PRIMARY KEY,     -- global monotonic cursor
  topic        text NOT NULL,             -- 'user'|'workspace'|'workspace_member'|'board'|'board_member'|'view'|'card'|'comment'|'message'|'attachment'|'invite'|'inbox'|'mention'|'notification'|'user_contact'
  entity_id    uuid NOT NULL,
  workspace_id uuid,
  board_id     uuid,
  op           text NOT NULL CHECK (op IN ('upsert','delete')),
  payload      jsonb NOT NULL,
  created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_outbox_topic          ON event_outbox(topic, created_at);
CREATE INDEX IF NOT EXISTS ix_outbox_board          ON event_outbox(board_id, created_at);
CREATE INDEX IF NOT EXISTS ix_outbox_created_cursor ON event_outbox(created_at, cursor);