-- ============================================================================
-- VIEWS
-- ============================================================================
CREATE TABLE IF NOT EXISTS views (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title      text NOT NULL,
  type       view_type NOT NULL,
  layout     jsonb NOT NULL DEFAULT '{}',
  position   int NOT NULL DEFAULT 0,
  version    int NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_views_board_pos ON views(board_id, position);
CREATE INDEX IF NOT EXISTS ix_views_deleted   ON views(deleted_at);
DROP TRIGGER IF EXISTS trg_views_upd ON views;
CREATE TRIGGER trg_views_upd     BEFORE UPDATE ON views FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_views_version ON views;
CREATE TRIGGER trg_views_version BEFORE UPDATE ON views FOR EACH ROW EXECUTE FUNCTION bump_version();