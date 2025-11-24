--Workspace bootstrap ownership
-- Ensure creator becomes owner on workspace creation
CREATE OR REPLACE FUNCTION trg_ws_bootstrap_owner()
RETURNS trigger AS $$
BEGIN
  IF NEW.created_by IS NOT NULL THEN
    INSERT INTO workspace_members(workspace_id, user_id, role, joined_at)
    VALUES (NEW.id, NEW.created_by, 'owner', now())
    ON CONFLICT (workspace_id, user_id) DO UPDATE
      SET role = GREATEST(workspace_members.role, EXCLUDED.role);
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_workspace_bootstrap_owner ON workspaces;
CREATE TRIGGER trg_workspace_bootstrap_owner
AFTER INSERT ON workspaces
FOR EACH ROW EXECUTE FUNCTION trg_ws_bootstrap_owner();


--Board membership consistency with workspace membership
-- Auto-ensure board members are always members of the parent workspace
CREATE OR REPLACE FUNCTION trg_board_members_ws_consistency()
RETURNS trigger AS $$
DECLARE
  ws uuid;
BEGIN
  -- get workspace from board
  SELECT workspace_id INTO ws FROM boards WHERE id = NEW.board_id;

  -- insert workspace_members row if missing
  INSERT INTO workspace_members(workspace_id, user_id, role, joined_at)
  VALUES (ws, NEW.user_id, 'member', now())
  ON CONFLICT (workspace_id, user_id) DO NOTHING;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_board_members_ws_consistency ON board_members;
CREATE TRIGGER trg_board_members_ws_consistency
BEFORE INSERT OR UPDATE ON board_members
FOR EACH ROW EXECUTE FUNCTION trg_board_members_ws_consistency();


COMMENT ON TABLE workspaces IS
  'NOTE: soft-delete cascade (boards → views/cards/comments/messages/etc.) '
  'is NOT enforced by triggers. A periodic worker must propagate deleted_at '
  'to downstream entities for eventual consistency.';
