CREATE TABLE IF NOT EXISTS event_outbox (
  cursor       BIGSERIAL PRIMARY KEY,
  topic        outbox_topic NOT NULL,
  entity_id    uuid NOT NULL,
  workspace_id uuid,
  board_id     uuid,
  op           outbox_operation NOT NULL,
  payload      jsonb NOT NULL,
  created_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_outbox_topic
  ON event_outbox(topic, created_at);

CREATE INDEX IF NOT EXISTS ix_outbox_board
  ON event_outbox(board_id, created_at);

CREATE INDEX IF NOT EXISTS ix_outbox_created_cursor
  ON event_outbox(created_at, cursor);
