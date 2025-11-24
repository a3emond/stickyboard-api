-- ============================================================================
-- USERS & AUTH
-- ============================================================================
CREATE TABLE IF NOT EXISTS users (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email        text NOT NULL UNIQUE,
  display_name text NOT NULL,
  avatar_uri   text,
  prefs        jsonb NOT NULL DEFAULT '{}',
  groups       text[] DEFAULT '{}',
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  deleted_at   timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_lower_email ON users(lower(email));
CREATE INDEX IF NOT EXISTS ix_users_deleted ON users(deleted_at);
DROP TRIGGER IF EXISTS trg_users_updated ON users;
CREATE TRIGGER trg_users_updated BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Local credential store / role
CREATE TABLE IF NOT EXISTS auth_users (
  user_id       uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  password_hash text NOT NULL,
  role          user_role NOT NULL DEFAULT 'user',
  last_login    timestamptz DEFAULT now(),
  created_at    timestamptz DEFAULT now(),
  updated_at    timestamptz DEFAULT now(),
  deleted_at    timestamptz
);
DROP TRIGGER IF EXISTS trg_auth_users_upd ON auth_users;
CREATE TRIGGER trg_auth_users_upd BEFORE UPDATE ON auth_users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Refresh tokens (opaque on the wire, hashed at rest)
CREATE TABLE IF NOT EXISTS refresh_tokens (
  token_hash  text PRIMARY KEY,                         -- sha256 of opaque refresh token
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  client_id   text,                                     -- device/app identifier (optional)
  user_agent  text,                                     -- audit (optional)
  ip_addr     inet,                                     -- audit (optional)
  issued_at   timestamptz NOT NULL DEFAULT now(),
  expires_at  timestamptz NOT NULL DEFAULT (now() + interval '30 days'),
  revoked     boolean NOT NULL DEFAULT false,
  revoked_at  timestamptz,
  replaced_by text                                      -- next token hash if rotated
);
CREATE INDEX IF NOT EXISTS ix_rt_user     ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS ix_rt_validity ON refresh_tokens(revoked, expires_at);