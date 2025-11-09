-- ============================================================================
-- ATTACHMENTS (CDN-BACKED METADATA)
-- ============================================================================

---------------------------------------------------
-- 1.1 attachments (original logical file)
---------------------------------------------------
CREATE TABLE attachments (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id    uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  board_id        uuid REFERENCES boards(id) ON DELETE CASCADE,
  card_id         uuid REFERENCES cards(id) ON DELETE SET NULL,

  filename        text NOT NULL,
  mime            text,
  byte_size       bigint,
  checksum_sha256 bytea,
  storage_path    text NOT NULL,
  is_public       boolean NOT NULL DEFAULT false,

  status          text NOT NULL DEFAULT 'ready',
  meta            jsonb NOT NULL DEFAULT '{}'::jsonb,

  uploaded_by     uuid REFERENCES users(id) ON DELETE SET NULL,
  version         int NOT NULL DEFAULT 0,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz
);

CREATE INDEX ix_attach_board     ON attachments(board_id);
CREATE INDEX ix_attach_card      ON attachments(card_id);
CREATE INDEX ix_attach_deleted   ON attachments(deleted_at);
CREATE INDEX ix_attach_workspace ON attachments(workspace_id, created_at DESC);
CREATE INDEX ix_attach_storage   ON attachments(storage_path);

CREATE TRIGGER trg_attach_upd
  BEFORE UPDATE ON attachments
  FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_attach_version
  BEFORE UPDATE ON attachments
  FOR EACH ROW EXECUTE FUNCTION bump_version();

