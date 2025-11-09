--------------------------------------------------
-- file_tokens (signed URL tokens / revocable access)
--------------------------------------------------
CREATE TABLE file_tokens (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  attachment_id uuid NOT NULL REFERENCES attachments(id) ON DELETE CASCADE,
  variant       text DEFAULT NULL,
  secret        bytea NOT NULL,
  audience      text NOT NULL DEFAULT 'download',
  expires_at    timestamptz NOT NULL,
  created_by    uuid REFERENCES users(id) ON DELETE SET NULL,
  revoked       boolean NOT NULL DEFAULT false,
  created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_file_tokens_valid
  ON file_tokens(attachment_id, expires_at)
  WHERE revoked = false;