-- ============================================================================
-- BOARDS & (OPTIONAL) BOARD-LEVEL MEMBERSHIP OVERRIDES
-- ============================================================================
CREATE TABLE IF NOT EXISTS boards (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  title        text NOT NULL,
  theme        jsonb NOT NULL DEFAULT '{}',
  meta         jsonb NOT NULL DEFAULT '{}',
  created_by   uuid REFERENCES users(id),
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  deleted_at   timestamptz
);
CREATE INDEX IF NOT EXISTS ix_boards_workspace ON boards(workspace_id);
CREATE INDEX IF NOT EXISTS ix_boards_deleted   ON boards(deleted_at);
DROP TRIGGER IF EXISTS trg_boards_upd ON boards;
CREATE TRIGGER trg_boards_upd BEFORE UPDATE ON boards FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Board-level role override (optional)
CREATE TABLE IF NOT EXISTS board_members (
  board_id uuid REFERENCES boards(id) ON DELETE CASCADE,
  user_id  uuid REFERENCES users(id) ON DELETE CASCADE,
  role     workspace_role,
  PRIMARY KEY(board_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_board_members_user ON board_members(user_id);