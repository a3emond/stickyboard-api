-- ============================================================================
-- ROUTING HELPERS FOR OUTBOX EMITTERS
-- ============================================================================
-- Resolve workspace_id from a board_id
CREATE OR REPLACE FUNCTION _sb_ws_from_board(p_board_id uuid)
RETURNS uuid AS $$
  SELECT b.workspace_id FROM boards b WHERE b.id = p_board_id;
$$ LANGUAGE sql STABLE;

-- Resolve (board_id, workspace_id) from a view_id
CREATE OR REPLACE FUNCTION _sb_board_ws_from_view(p_view_id uuid)
RETURNS TABLE(board_id uuid, workspace_id uuid) AS $$
  SELECT v.board_id, b.workspace_id
  FROM views v
  JOIN boards b ON b.id = v.board_id
  WHERE v.id = p_view_id;
$$ LANGUAGE sql STABLE;

-- Resolve (board_id, workspace_id) from a card_id
CREATE OR REPLACE FUNCTION _sb_board_ws_from_card(p_card_id uuid)
RETURNS TABLE(board_id uuid, workspace_id uuid) AS $$
  SELECT c.board_id, b.workspace_id
  FROM cards c
  JOIN boards b ON b.id = c.board_id
  WHERE c.id = p_card_id;
$$ LANGUAGE sql STABLE;