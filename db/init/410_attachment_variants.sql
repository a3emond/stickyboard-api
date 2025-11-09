--------------------------------------------------
-- attachment_variants (worker-generated previews)
--------------------------------------------------
CREATE TABLE attachment_variants (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  parent_id       uuid NOT NULL REFERENCES attachments(id) ON DELETE CASCADE,

  variant         text NOT NULL,
  mime            text NOT NULL,
  byte_size       bigint,
  width           int,
  height          int,
  duration_ms     int,
  storage_path    text NOT NULL,
  status          text NOT NULL DEFAULT 'ready',
  checksum_sha256 bytea,

  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_variant_unique
  ON attachment_variants(parent_id, variant);

CREATE INDEX ix_variant_parent
  ON attachment_variants(parent_id);

-- Safety: disable triggers on attachment_variants (if someone adds one accidentally)
ALTER TABLE attachment_variants DISABLE TRIGGER ALL;