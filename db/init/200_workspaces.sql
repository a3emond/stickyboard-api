-- ============================================================================
-- WORKSPACES & MEMBERSHIP
-- ============================================================================
CREATE TABLE IF NOT EXISTS workspaces (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name       text NOT NULL,
  created_by uuid REFERENCES users(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_workspaces_deleted ON workspaces(deleted_at);
DROP TRIGGER IF EXISTS trg_ws_upd ON workspaces;
CREATE TRIGGER trg_ws_upd BEFORE UPDATE ON workspaces FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS workspace_members (
  workspace_id uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  user_id      uuid REFERENCES users(id) ON DELETE CASCADE,
  role         workspace_role NOT NULL DEFAULT 'member',
  joined_at    timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(workspace_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_ws_members_user ON workspace_members(user_id);