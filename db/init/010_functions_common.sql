-- ============================================================================
-- UTILITIES
-- ============================================================================

-- Touch updated_at on every UPDATE of tables that use it
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END; $$ LANGUAGE plpgsql;

-- Bump integer version column on UPDATE of versioned tables
CREATE OR REPLACE FUNCTION bump_version() RETURNS trigger AS $$
BEGIN
  NEW.version := COALESCE(OLD.version, 0) + 1;
  RETURN NEW;
END; $$ LANGUAGE plpgsql;

-- Convenience hash helper (store only token hashes, never plaintext)
CREATE OR REPLACE FUNCTION sha256_hex(t text) RETURNS text AS $$
  SELECT encode(digest(t, 'sha256'), 'hex');
$$ LANGUAGE sql IMMUTABLE;