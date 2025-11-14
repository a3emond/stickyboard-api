-- ============================================================================
-- USER CONTACTS (friend / follow system)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_contacts (
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  contact_id  uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  status      contact_status NOT NULL DEFAULT 'pending', -- pending | accepted | blocked
  created_at  timestamptz NOT NULL DEFAULT now(),
  accepted_at timestamptz,
  updated_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, contact_id)
);
CREATE INDEX IF NOT EXISTS ix_user_contacts_contact ON user_contacts(contact_id);
CREATE INDEX IF NOT EXISTS ix_user_contacts_status  ON user_contacts(status);
DROP TRIGGER IF EXISTS trg_user_contacts_upd ON user_contacts;
CREATE TRIGGER trg_user_contacts_upd BEFORE UPDATE ON user_contacts FOR EACH ROW EXECUTE FUNCTION set_updated_at();


