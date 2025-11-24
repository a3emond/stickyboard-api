-- ============================================================================
-- INVITES (WORKSPACE / BOARD / CONTACT; HASHED TOKEN)
-- ============================================================================
CREATE TABLE IF NOT EXISTS invites (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  email         text NOT NULL,                                        -- supports non-users

  scope_type    invite_scope NOT NULL,                                -- 'workspace'|'board'|'contact'
  workspace_id  uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  board_id      uuid REFERENCES boards(id)     ON DELETE CASCADE,
  contact_id    uuid REFERENCES users(id)      ON DELETE CASCADE,      -- optional; can be null for non-user contact

  target_role   workspace_role,                                       -- for workspace scope
  board_role    workspace_role,                                       -- optional board override

  token_hash    text NOT NULL UNIQUE,                                 -- store hash of opaque token
  status        invite_status NOT NULL DEFAULT 'pending',
  accepted_by   uuid REFERENCES users(id),
  accepted_at   timestamptz,
  revoked_at    timestamptz,
  created_at    timestamptz NOT NULL DEFAULT now(),
  expires_at    timestamptz NOT NULL DEFAULT (now() + interval '7 days'),
  note          text,

  -- Exact scope behaviour:
  -- workspace: workspace_id set, others null
  -- board:     board_id set, others null
  -- contact:   workspace_id/board_id null, contact_id optional (existing user) or null (email-only)
  CONSTRAINT invites_scope_valid CHECK (
    (scope_type = 'workspace' AND workspace_id IS NOT NULL AND board_id IS NULL AND contact_id IS NULL) OR
    (scope_type = 'board'     AND board_id     IS NOT NULL AND workspace_id IS NULL AND contact_id IS NULL) OR
    (scope_type = 'contact'   AND workspace_id IS NULL     AND board_id     IS NULL)
  ),

  CONSTRAINT invites_workspace_role_req CHECK (
    scope_type <> 'workspace' OR target_role IS NOT NULL
  )
);

-- Prevent duplicate pending invite for same email+scope (including contact)
CREATE UNIQUE INDEX IF NOT EXISTS idx_invites_email_scope
  ON invites(
      email,
      scope_type,
      COALESCE(workspace_id, '00000000-0000-0000-0000-000000000000'::uuid),
      COALESCE(board_id,     '00000000-0000-0000-0000-000000000000'::uuid),
      COALESCE(contact_id,   '00000000-0000-0000-0000-000000000000'::uuid)
  )
  WHERE status = 'pending';

CREATE INDEX IF NOT EXISTS ix_invites_scope_time 
  ON invites(scope_type, workspace_id, board_id, contact_id, created_at);

CREATE INDEX IF NOT EXISTS ix_invites_status_exp 
  ON invites(status, expires_at);
