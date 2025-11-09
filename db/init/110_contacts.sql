-- ============================================================================
-- USER CONTACTS (friend / follow system)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_contacts (
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  contact_id  uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  status      text NOT NULL DEFAULT 'pending', -- pending | accepted | blocked
  created_at  timestamptz NOT NULL DEFAULT now(),
  accepted_at timestamptz,
  PRIMARY KEY (user_id, contact_id)
);
CREATE INDEX IF NOT EXISTS ix_user_contacts_contact ON user_contacts(contact_id);
CREATE INDEX IF NOT EXISTS ix_user_contacts_status  ON user_contacts(status);