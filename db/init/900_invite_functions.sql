-- ============================================================================
-- INVITE HELPERS (SERVER-SIDE CONVENIENCE)
-- ============================================================================

-- Create an invite row given a pre-hashed token (hash on app side or via sha256_hex)
CREATE OR REPLACE FUNCTION invite_create(
  p_sender       uuid,
  p_email        text,
  p_scope_type   invite_scope,
  p_workspace    uuid,
  p_board        uuid,
  p_contact      uuid,
  p_target_role  workspace_role,
  p_board_role   workspace_role,
  p_token_hash   text,
  p_expires_in   interval DEFAULT interval '7 days',
  p_note         text DEFAULT NULL
) RETURNS uuid AS $$
DECLARE 
  v_id uuid;
BEGIN
  INSERT INTO invites (
    id,
    sender_id,
    email,
    scope_type,
    workspace_id,
    board_id,
    contact_id,
    target_role,
    board_role,
    token_hash,
    expires_at,
    note
  )
  VALUES (
    gen_random_uuid(),
    p_sender,
    p_email,
    p_scope_type,
    p_workspace,
    p_board,
    p_contact,
    p_target_role,
    p_board_role,
    p_token_hash,
    now() + p_expires_in,
    p_note
  )
  RETURNING id INTO v_id;

  RETURN v_id;
END; $$ LANGUAGE plpgsql;


-- Accept an invite by token hash and upsert membership / contacts
CREATE OR REPLACE FUNCTION invite_accept(
  p_token_hash     text,
  p_accepting_user uuid
) RETURNS TABLE(
  invite_id    uuid,
  scope_type   invite_scope,
  workspace_id uuid,
  board_id     uuid,
  contact_id   uuid,
  target_role  workspace_role,
  board_role   workspace_role
) AS $$
DECLARE 
  v_inv invites;
BEGIN
  SELECT * INTO v_inv
  FROM invites
  WHERE token_hash = p_token_hash
    AND status = 'pending'
    AND now() < expires_at
  FOR UPDATE;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'invalid_or_expired_invite';
  END IF;

  -- Workspace membership
  IF v_inv.scope_type = 'workspace' AND v_inv.workspace_id IS NOT NULL THEN
    INSERT INTO workspace_members(workspace_id, user_id, role)
    VALUES (v_inv.workspace_id, p_accepting_user, COALESCE(v_inv.target_role, 'member'))
    ON CONFLICT (workspace_id, user_id) DO UPDATE
      SET role = GREATEST(EXCLUDED.role, workspace_members.role);
  END IF;

  -- Board membership
  IF v_inv.scope_type = 'board' AND v_inv.board_id IS NOT NULL THEN
    INSERT INTO board_members(board_id, user_id, role)
    VALUES (v_inv.board_id, p_accepting_user, COALESCE(v_inv.board_role, 'member'))
    ON CONFLICT (board_id, user_id) DO UPDATE
      SET role = GREATEST(EXCLUDED.role, board_members.role);
  END IF;

  -- Contact friendship reciprocity
  IF v_inv.scope_type = 'contact' THEN
    -- If the invite was created for a specific known user, use that
    -- Otherwise, treat sender and accepting user as contacts.
    -- We always create a symmetric friendship between sender and accepter.
    INSERT INTO user_contacts(user_id, contact_id, status, created_at, accepted_at)
    VALUES (v_inv.sender_id, p_accepting_user, 'accepted', now(), now())
    ON CONFLICT (user_id, contact_id) DO UPDATE
      SET status      = 'accepted',
          accepted_at = COALESCE(user_contacts.accepted_at, EXCLUDED.accepted_at);

    INSERT INTO user_contacts(user_id, contact_id, status, created_at, accepted_at)
    VALUES (p_accepting_user, v_inv.sender_id, 'accepted', now(), now())
    ON CONFLICT (user_id, contact_id) DO UPDATE
      SET status      = 'accepted',
          accepted_at = COALESCE(user_contacts.accepted_at, EXCLUDED.accepted_at);
  END IF;

  -- Mark invite as accepted
  UPDATE invites
     SET status      = 'accepted',
         accepted_at = now(),
         accepted_by = p_accepting_user
   WHERE id = v_inv.id;

  RETURN QUERY
  SELECT
    v_inv.id,
    v_inv.scope_type,
    v_inv.workspace_id,
    v_inv.board_id,
    v_inv.contact_id,
    v_inv.target_role,
    v_inv.board_role;
END; $$ LANGUAGE plpgsql;


-- Revoke a pending invite
CREATE OR REPLACE FUNCTION invite_revoke(p_token_hash text) 
RETURNS void AS $$
BEGIN
  UPDATE invites 
     SET status    = 'revoked',
         revoked_at = now()
   WHERE token_hash = p_token_hash
     AND status     = 'pending';
END; $$ LANGUAGE plpgsql;
