-- ============================================================================
-- CHAT (BOARD/VIEW) & INBOX (DIRECT)
-- ============================================================================
-- NOTE: channel 'direct' exists in the enum, but is RESERVED for the inbox tables below.
-- This table enforces that only board/view/system are stored here.
CREATE TABLE IF NOT EXISTS messages (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  channel    message_channel NOT NULL CHECK (channel IN ('board','view','system')),
  board_id   uuid REFERENCES boards(id) ON DELETE CASCADE,
  view_id    uuid REFERENCES views(id) ON DELETE CASCADE,
  sender_id  uuid REFERENCES users(id),
  content    text NOT NULL,
  parent_id  uuid REFERENCES messages(id) ON DELETE SET NULL, -- quote/reply
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_messages_board        ON messages(board_id, created_at);
CREATE INDEX IF NOT EXISTS ix_messages_view         ON messages(view_id, created_at);
CREATE INDEX IF NOT EXISTS ix_messages_deleted      ON messages(deleted_at);
CREATE INDEX IF NOT EXISTS ix_messages_board_time   ON messages(board_id, created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_messages_view_time    ON messages(view_id, created_at DESC)  WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_msg_upd ON messages;
CREATE TRIGGER trg_msg_upd BEFORE UPDATE ON messages FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Direct messages/inbox
CREATE TABLE IF NOT EXISTS inbox_messages (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id   uuid REFERENCES users(id) ON DELETE SET NULL,
  receiver_id uuid REFERENCES users(id) ON DELETE CASCADE,
  content     text NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  read_at     timestamptz
);
CREATE INDEX IF NOT EXISTS ix_inbox_receiver ON inbox_messages(receiver_id, created_at);