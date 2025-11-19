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

--Add a trigger preventing variants for FAILED attachments
CREATE OR REPLACE FUNCTION trg_block_variants_when_failed()
RETURNS trigger AS $$
DECLARE
  v_status text;
BEGIN
  SELECT status INTO v_status
  FROM attachments
  WHERE id = NEW.parent_id;

  IF v_status = 'failed' THEN
    RAISE EXCEPTION 'variants-disabled: attachment % is failed', NEW.parent_id;
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_variant_block_failed ON attachment_variants;
CREATE TRIGGER trg_variant_block_failed
BEFORE INSERT ON attachment_variants
FOR EACH ROW EXECUTE FUNCTION trg_block_variants_when_failed();

-- NOTE FOR WORKERS:
-- attachments.status state machine:
--   ready  -> valid storage_path required
--   failed -> no new variants allowed (enforced by trg_variant_block_failed)
-- Workers should:
--   - stop generating thumbnails/transcodes when status='failed'
--   - mark attachments failed when any variant pipeline chain irrecoverably fails
--   - optionally clean partial variants for failed attachments
