-- ============================================================================
-- MENTIONS & NOTIFICATIONS
-- ============================================================================
CREATE TABLE IF NOT EXISTS mentions (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_type    entity_type NOT NULL CHECK (entity_type IN ('card','comment','message','doc','whiteboard')),
  entity_id      uuid NOT NULL,
  mentioned_user uuid NOT NULL REFERENCES users(id),
  author_id      uuid REFERENCES users(id),
  created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_mentions_user ON mentions(mentioned_user, created_at);

CREATE TABLE IF NOT EXISTS notifications (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    uuid REFERENCES users(id),
  type       notification_type NOT NULL,
  entity_id  uuid,
  read       boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  read_at    timestamptz
);
CREATE INDEX IF NOT EXISTS ix_notif_user ON notifications(user_id, read, created_at);
