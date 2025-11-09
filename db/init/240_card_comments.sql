-- ============================================================================
-- CARD COMMENTS (THREADED)
-- ============================================================================
CREATE TABLE IF NOT EXISTS card_comments (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  parent_id  uuid REFERENCES card_comments(id) ON DELETE SET NULL,
  user_id    uuid REFERENCES users(id),
  content    text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_comments_card          ON card_comments(card_id, created_at);
CREATE INDEX IF NOT EXISTS ix_card_comments_deleted  ON card_comments(deleted_at);
CREATE INDEX IF NOT EXISTS ix_card_comments_time     ON card_comments(card_id, created_at DESC) WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_cc_upd ON card_comments;
CREATE TRIGGER trg_cc_upd BEFORE UPDATE ON card_comments FOR EACH ROW EXECUTE FUNCTION set_updated_at();