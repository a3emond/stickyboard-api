-- ============================================================================
-- CARDS
-- ============================================================================
CREATE TABLE IF NOT EXISTS cards (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id       uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title          text,
  markdown       text NOT NULL DEFAULT '',
  ink_data       jsonb,
  due_date       timestamptz,
  start_date     timestamptz,
  end_date       timestamptz,
  checklist      jsonb,
  priority       int,
  status         card_status NOT NULL DEFAULT 'open',
  tags           text[] NOT NULL DEFAULT '{}',
  assignee       uuid REFERENCES users(id),
  created_by     uuid REFERENCES users(id),
  last_edited_by uuid REFERENCES users(id),
  version        int NOT NULL DEFAULT 0,
  created_at     timestamptz NOT NULL DEFAULT now(),
  updated_at     timestamptz NOT NULL DEFAULT now(),
  deleted_at     timestamptz
);
CREATE INDEX IF NOT EXISTS ix_cards_board          ON cards(board_id);
CREATE INDEX IF NOT EXISTS ix_cards_status         ON cards(status, updated_at);
CREATE INDEX IF NOT EXISTS ix_cards_deleted        ON cards(deleted_at);
CREATE INDEX IF NOT EXISTS ix_cards_board_updated  ON cards(board_id, updated_at DESC) WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_cards_upd ON cards;
CREATE TRIGGER trg_cards_upd     BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_cards_version ON cards;
CREATE TRIGGER trg_cards_version BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION bump_version();

-- Per-user read checkpoint for card threads
CREATE TABLE IF NOT EXISTS card_reads (
  card_id      uuid REFERENCES cards(id) ON DELETE CASCADE,
  user_id      uuid REFERENCES users(id) ON DELETE CASCADE,
  last_seen_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(card_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_card_reads_user ON card_reads(user_id, last_seen_at);