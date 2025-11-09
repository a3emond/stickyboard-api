-- ============================================================================
-- INVITES (WORKSPACE OR BOARD; HASHED TOKEN)
-- ============================================================================
CREATE TABLE IF NOT EXISTS invites (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  email         text NOT NULL,                                        -- supports non-users
  workspace_id  uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  board_id      uuid REFERENCES boards(id)     ON DELETE CASCADE,
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
  CONSTRAINT invites_scope_xor CHECK ((workspace_id IS NOT NULL)::int + (board_id IS NOT NULL)::int = 1),
  CONSTRAINT invites_workspace_role_req CHECK (workspace_id IS NULL OR target_role IS NOT NULL)
);
-- Prevent duplicate pending invite for same email+scope
CREATE UNIQUE INDEX IF NOT EXISTS idx_invites_email_scope
  ON invites(email, COALESCE(workspace_id, '00000000-0000-0000-0000-000000000000'::uuid), COALESCE(board_id, '00000000-0000-0000-0000-000000000000'::uuid))
  WHERE status = 'pending';
CREATE INDEX IF NOT EXISTS ix_invites_scope_time ON invites(workspace_id, board_id, created_at);
CREATE INDEX IF NOT EXISTS ix_invites_status_exp ON invites(status, expires_at);