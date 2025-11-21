-- StickyBoard DB Schema (Final Freeze, Reinforced)
-- Purpose: Collaborative workspaces with cards, views, threaded comments, chat, inbox,
-- mentions, notifications, attachments, invites, refresh tokens, contacts, and full
-- sync/realtime infrastructure with event outbox and worker queue.

-- ============================================================================
-- EXTENSIONS & UTILITIES
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

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

-- ============================================================================
-- ENUM TYPES
-- ============================================================================
DO $$ BEGIN CREATE TYPE user_role         AS ENUM ('user','admin','moderator');                    EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE workspace_role    AS ENUM ('owner','admin','moderator','member','guest','none');              EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE view_type         AS ENUM ('kanban','list','calendar','timeline','metrics','doc','whiteboard','chat'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE card_status       AS ENUM ('open','in_progress','blocked','done','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE message_channel   AS ENUM ('board','view','direct','system');              EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE notification_type AS ENUM ('mention','reply','assignment','system');       EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE invite_status     AS ENUM ('pending','accepted','revoked','expired');      EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE contact_status    AS ENUM ('pending','accepted','blocked')                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE invite_scope    AS ENUM ('workspace','board','contact')                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;

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



-- ============================================================================
-- WORKSPACES & MEMBERSHIP
-- ============================================================================
CREATE TABLE IF NOT EXISTS workspaces (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name       text NOT NULL,
  created_by uuid REFERENCES users(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_workspaces_deleted ON workspaces(deleted_at);
DROP TRIGGER IF EXISTS trg_ws_upd ON workspaces;
CREATE TRIGGER trg_ws_upd BEFORE UPDATE ON workspaces FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS workspace_members (
  workspace_id uuid REFERENCES workspaces(id) ON DELETE CASCADE,
  user_id      uuid REFERENCES users(id) ON DELETE CASCADE,
  role         workspace_role NOT NULL DEFAULT 'member',
  joined_at    timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(workspace_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_ws_members_user ON workspace_members(user_id);

-- ============================================================================
-- BOARDS & (OPTIONAL) BOARD-LEVEL MEMBERSHIP OVERRIDES
-- ============================================================================
CREATE TABLE IF NOT EXISTS boards (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
  title        text NOT NULL,
  theme        jsonb NOT NULL DEFAULT '{}',
  meta         jsonb NOT NULL DEFAULT '{}',
  created_by   uuid REFERENCES users(id),
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  deleted_at   timestamptz
);
CREATE INDEX IF NOT EXISTS ix_boards_workspace ON boards(workspace_id);
CREATE INDEX IF NOT EXISTS ix_boards_deleted   ON boards(deleted_at);
DROP TRIGGER IF EXISTS trg_boards_upd ON boards;
CREATE TRIGGER trg_boards_upd BEFORE UPDATE ON boards FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Board-level role override (optional)
CREATE TABLE IF NOT EXISTS board_members (
  board_id uuid REFERENCES boards(id) ON DELETE CASCADE,
  user_id  uuid REFERENCES users(id) ON DELETE CASCADE,
  role     workspace_role,
  PRIMARY KEY(board_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_board_members_user ON board_members(user_id);

-- ============================================================================
-- VIEWS
-- ============================================================================
CREATE TABLE IF NOT EXISTS views (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title      text NOT NULL,
  type       view_type NOT NULL,
  layout     jsonb NOT NULL DEFAULT '{}',
  position   int NOT NULL DEFAULT 0,
  version    int NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_views_board_pos ON views(board_id, position);
CREATE INDEX IF NOT EXISTS ix_views_deleted   ON views(deleted_at);
DROP TRIGGER IF EXISTS trg_views_upd ON views;
CREATE TRIGGER trg_views_upd     BEFORE UPDATE ON views FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_views_version ON views;
CREATE TRIGGER trg_views_version BEFORE UPDATE ON views FOR EACH ROW EXECUTE FUNCTION bump_version();

-- ============================================================================
-- CARDS
-- ============================================================================
CREATE TABLE IF NOT EXISTS cards (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id       uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title          text,
  markdown       text NOT NULL DEFAULT '',
  ink_data       jsonb,
  due_date       timestamptz,
  start_date     timestamptz,
  end_date       timestamptz,
  checklist      jsonb,
  priority       int,
  status         card_status NOT NULL DEFAULT 'open',
  tags           text[] NOT NULL DEFAULT '{}',
  assignee       uuid REFERENCES users(id),
  created_by     uuid REFERENCES users(id),
  last_edited_by uuid REFERENCES users(id),
  version        int NOT NULL DEFAULT 0,
  created_at     timestamptz NOT NULL DEFAULT now(),
  updated_at     timestamptz NOT NULL DEFAULT now(),
  deleted_at     timestamptz
);
CREATE INDEX IF NOT EXISTS ix_cards_board          ON cards(board_id);
CREATE INDEX IF NOT EXISTS ix_cards_status         ON cards(status, updated_at);
CREATE INDEX IF NOT EXISTS ix_cards_deleted        ON cards(deleted_at);
CREATE INDEX IF NOT EXISTS ix_cards_board_updated  ON cards(board_id, updated_at DESC) WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_cards_upd ON cards;
CREATE TRIGGER trg_cards_upd     BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_cards_version ON cards;
CREATE TRIGGER trg_cards_version BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION bump_version();

-- Per-user read checkpoint for card threads
CREATE TABLE IF NOT EXISTS card_reads (
  card_id      uuid REFERENCES cards(id) ON DELETE CASCADE,
  user_id      uuid REFERENCES users(id) ON DELETE CASCADE,
  last_seen_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(card_id, user_id)
);
CREATE INDEX IF NOT EXISTS ix_card_reads_user ON card_reads(user_id, last_seen_at);

-- ============================================================================
-- CARD COMMENTS (THREADED)
-- ============================================================================
CREATE TABLE IF NOT EXISTS card_comments (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  parent_id  uuid REFERENCES card_comments(id) ON DELETE SET NULL,
  user_id    uuid REFERENCES users(id),
  content    text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_comments_card          ON card_comments(card_id, created_at);
CREATE INDEX IF NOT EXISTS ix_card_comments_deleted  ON card_comments(deleted_at);
CREATE INDEX IF NOT EXISTS ix_card_comments_time     ON card_comments(card_id, created_at DESC) WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_cc_upd ON card_comments;
CREATE TRIGGER trg_cc_upd BEFORE UPDATE ON card_comments FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ============================================================================
-- CHAT (BOARD/VIEW) & INBOX (DIRECT)
-- ============================================================================
-- NOTE: channel 'direct' exists in the enum, but is RESERVED for the inbox tables below.
-- This table enforces that only board/view/system are stored here.
CREATE TABLE IF NOT EXISTS messages (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  channel    message_channel NOT NULL CHECK (channel IN ('board','view','system')),
  board_id   uuid REFERENCES boards(id) ON DELETE CASCADE,
  view_id    uuid REFERENCES views(id) ON DELETE CASCADE,
  sender_id  uuid REFERENCES users(id),
  content    text NOT NULL,
  parent_id  uuid REFERENCES messages(id) ON DELETE SET NULL, -- quote/reply
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_messages_board        ON messages(board_id, created_at);
CREATE INDEX IF NOT EXISTS ix_messages_view         ON messages(view_id, created_at);
CREATE INDEX IF NOT EXISTS ix_messages_deleted      ON messages(deleted_at);
CREATE INDEX IF NOT EXISTS ix_messages_board_time   ON messages(board_id, created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_messages_view_time    ON messages(view_id, created_at DESC)  WHERE deleted_at IS NULL;
DROP TRIGGER IF EXISTS trg_msg_upd ON messages;
CREATE TRIGGER trg_msg_upd BEFORE UPDATE ON messages FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Direct messages/inbox
CREATE TABLE IF NOT EXISTS inbox_messages (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id   uuid REFERENCES users(id) ON DELETE SET NULL,
  receiver_id uuid REFERENCES users(id) ON DELETE CASCADE,
  content     text NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  read_at     timestamptz
);
CREATE INDEX IF NOT EXISTS ix_inbox_receiver ON inbox_messages(receiver_id, created_at);

-- ============================================================================
-- MENTIONS & NOTIFICATIONS
-- ============================================================================
CREATE TABLE IF NOT EXISTS mentions (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_type    text NOT NULL CHECK (entity_type IN ('card','comment','message','doc','whiteboard')),
  entity_id      uuid NOT NULL,
  mentioned_user uuid NOT NULL REFERENCES users(id),
  author_id      uuid REFERENCES users(id),
  created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_mentions_user ON mentions(mentioned_user, created_at);

CREATE TABLE IF NOT EXISTS notifications (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    uuid REFERENCES users(id),
  type       notification_type NOT NULL,
  entity_id  uuid,
  read       boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  read_at    timestamptz
);
CREATE INDEX IF NOT EXISTS ix_notif_user ON notifications(user_id, read, created_at);

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


-- ============================================================================
-- SYNC PRIMITIVES (OUTBOX, CURSORS, WORKERS, REALTIME)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_outbox (
  cursor       BIGSERIAL PRIMARY KEY,     -- global monotonic cursor
  topic        text NOT NULL,             -- 'user'|'workspace'|'workspace_member'|'board'|'board_member'|'view'|'card'|'comment'|'message'|'attachment'|'invite'|'inbox'|'mention'|'notification'|'user_contact'
  entity_id    uuid NOT NULL,
  workspace_id uuid,
  board_id     uuid,
  op           text NOT NULL CHECK (op IN ('upsert','delete')),
  payload      jsonb NOT NULL,
  created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_outbox_topic          ON event_outbox(topic, created_at);
CREATE INDEX IF NOT EXISTS ix_outbox_board          ON event_outbox(board_id, created_at);
CREATE INDEX IF NOT EXISTS ix_outbox_created_cursor ON event_outbox(created_at, cursor);

CREATE TABLE IF NOT EXISTS sync_cursors (
  user_id     uuid NOT NULL,
  scope_type  text NOT NULL CHECK (scope_type IN ('workspace','board','inbox')),
  scope_id    uuid,
  last_cursor bigint NOT NULL DEFAULT 0,
  updated_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(user_id, scope_type, scope_id)
);
CREATE INDEX IF NOT EXISTS ix_sync_cursors_scope ON sync_cursors(scope_type, scope_id);

-- Worker Jobs (generic async queue)
CREATE TABLE IF NOT EXISTS worker_jobs (
  id           bigserial PRIMARY KEY,
  kind         text NOT NULL,                       -- 'mention_notify'|'due_reminder'|'search_index'|'analytics_rollup'|'cdn_gc'|'invite_email'
  payload      jsonb NOT NULL,
  status       text NOT NULL DEFAULT 'queued',      -- 'queued'|'running'|'done'|'dead'
  attempts     int NOT NULL DEFAULT 0,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  available_at timestamptz NOT NULL DEFAULT now(),
  last_error   text
);
CREATE INDEX IF NOT EXISTS ix_jobs_ready ON worker_jobs(status, available_at);

CREATE TABLE IF NOT EXISTS worker_job_attempts (
  id          bigserial PRIMARY KEY,
  job_id      bigint NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
  started_at  timestamptz NOT NULL,
  finished_at timestamptz,
  error       text
);

-- Push tokens for notifications
CREATE TABLE IF NOT EXISTS push_tokens (
  id         bigserial PRIMARY KEY,
  user_id    uuid NOT NULL,
  provider   text NOT NULL CHECK (provider IN ('fcm','apns','webpush')),
  token      text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(provider, token)
);
CREATE INDEX IF NOT EXISTS ix_push_user ON push_tokens(user_id);

-- Realtime websocket session tracking (kept for future-proofing; Firebase is primary bus)
CREATE TABLE IF NOT EXISTS ws_sessions (
  id           bigserial PRIMARY KEY,
  user_id      uuid NOT NULL,
  node_id      text NOT NULL,
  connected_at timestamptz NOT NULL DEFAULT now(),
  last_seen_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ws_user ON ws_sessions(user_id);

-- ============================================================================
-- ROUTING HELPERS FOR OUTBOX EMITTERS
-- ============================================================================
-- Resolve workspace_id from a board_id
CREATE OR REPLACE FUNCTION _sb_ws_from_board(p_board_id uuid)
RETURNS uuid AS $$
  SELECT b.workspace_id FROM boards b WHERE b.id = p_board_id;
$$ LANGUAGE sql STABLE;

-- Resolve (board_id, workspace_id) from a view_id
CREATE OR REPLACE FUNCTION _sb_board_ws_from_view(p_view_id uuid)
RETURNS TABLE(board_id uuid, workspace_id uuid) AS $$
  SELECT v.board_id, b.workspace_id
  FROM views v
  JOIN boards b ON b.id = v.board_id
  WHERE v.id = p_view_id;
$$ LANGUAGE sql STABLE;

-- Resolve (board_id, workspace_id) from a card_id
CREATE OR REPLACE FUNCTION _sb_board_ws_from_card(p_card_id uuid)
RETURNS TABLE(board_id uuid, workspace_id uuid) AS $$
  SELECT c.board_id, b.workspace_id
  FROM cards c
  JOIN boards b ON b.id = c.board_id
  WHERE c.id = p_card_id;
$$ LANGUAGE sql STABLE;

-- ============================================================================
-- OUTBOX EMITTERS (TRIGGERS) FOR ALL ENTITIES
-- ============================================================================

-- USERS
CREATE OR REPLACE FUNCTION emit_user_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload, created_at)
    VALUES ('user', NEW.id, 'upsert', to_jsonb(NEW), now());
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload, created_at)
    VALUES ('user', OLD.id, 'delete', to_jsonb(OLD), now());
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_outbox ON users;
CREATE TRIGGER trg_users_outbox
AFTER INSERT OR UPDATE OR DELETE ON users
FOR EACH ROW EXECUTE FUNCTION emit_user_outbox();

-- WORKSPACES
CREATE OR REPLACE FUNCTION emit_workspace_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, op, payload)
    VALUES ('workspace', NEW.id, NEW.id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, op, payload)
    VALUES ('workspace', OLD.id, OLD.id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_workspaces_outbox ON workspaces;
CREATE TRIGGER trg_workspaces_outbox
AFTER INSERT OR UPDATE OR DELETE ON workspaces
FOR EACH ROW EXECUTE FUNCTION emit_workspace_outbox();

-- WORKSPACE MEMBERS
CREATE OR REPLACE FUNCTION emit_workspace_member_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, op, payload)
    VALUES ('workspace_member', NEW.user_id, NEW.workspace_id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, op, payload)
    VALUES ('workspace_member', OLD.user_id, OLD.workspace_id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_ws_members_outbox ON workspace_members;
CREATE TRIGGER trg_ws_members_outbox
AFTER INSERT OR UPDATE OR DELETE ON workspace_members
FOR EACH ROW EXECUTE FUNCTION emit_workspace_member_outbox();

-- BOARDS
CREATE OR REPLACE FUNCTION emit_board_outbox()
RETURNS trigger AS $$
DECLARE ws uuid;
BEGIN
  ws := COALESCE(NEW.workspace_id, OLD.workspace_id);
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('board', NEW.id, ws, NEW.id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('board', OLD.id, ws, OLD.id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_boards_outbox ON boards;
CREATE TRIGGER trg_boards_outbox
AFTER INSERT OR UPDATE OR DELETE ON boards
FOR EACH ROW EXECUTE FUNCTION emit_board_outbox();

-- BOARD MEMBERS
CREATE OR REPLACE FUNCTION emit_board_member_outbox()
RETURNS trigger AS $$
DECLARE ws uuid;
BEGIN
  SELECT workspace_id INTO ws FROM boards WHERE id = COALESCE(NEW.board_id, OLD.board_id);

  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('board_member', NEW.user_id, ws, NEW.board_id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('board_member', OLD.user_id, ws, OLD.board_id, 'delete', to_jsonb(OLD));
  END IF;

  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_board_members_outbox ON board_members;
CREATE TRIGGER trg_board_members_outbox
AFTER INSERT OR UPDATE OR DELETE ON board_members
FOR EACH ROW EXECUTE FUNCTION emit_board_member_outbox();

-- VIEWS
CREATE OR REPLACE FUNCTION emit_view_outbox()
RETURNS trigger AS $$
DECLARE b uuid; ws uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    b := NEW.board_id; ws := _sb_ws_from_board(b);
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('view', NEW.id, ws, b, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    b := OLD.board_id; ws := _sb_ws_from_board(b);
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('view', OLD.id, ws, b, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_views_outbox ON views;
CREATE TRIGGER trg_views_outbox
AFTER INSERT OR UPDATE OR DELETE ON views
FOR EACH ROW EXECUTE FUNCTION emit_view_outbox();

-- CARDS
CREATE OR REPLACE FUNCTION emit_card_outbox()
RETURNS trigger AS $$
DECLARE b uuid; ws uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    b := NEW.board_id; ws := _sb_ws_from_board(b);
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('card', NEW.id, ws, b, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    b := OLD.board_id; ws := _sb_ws_from_board(b);
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('card', OLD.id, ws, b, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_cards_outbox ON cards;
CREATE TRIGGER trg_cards_outbox
AFTER INSERT OR UPDATE OR DELETE ON cards
FOR EACH ROW EXECUTE FUNCTION emit_card_outbox();

-- CARD COMMENTS
CREATE OR REPLACE FUNCTION emit_card_comment_outbox()
RETURNS trigger AS $$
DECLARE b uuid; ws uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    SELECT board_id, workspace_id INTO b, ws FROM _sb_board_ws_from_card(COALESCE(NEW.card_id, OLD.card_id));
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('comment', NEW.id, ws, b, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    SELECT board_id, workspace_id INTO b, ws FROM _sb_board_ws_from_card(OLD.card_id);
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('comment', OLD.id, ws, b, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_card_comments_outbox ON card_comments;
CREATE TRIGGER trg_card_comments_outbox
AFTER INSERT OR UPDATE OR DELETE ON card_comments
FOR EACH ROW EXECUTE FUNCTION emit_card_comment_outbox();

-- MESSAGES (board/view/system)
CREATE OR REPLACE FUNCTION emit_message_outbox()
RETURNS trigger AS $$
DECLARE b uuid; ws uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    b := NEW.board_id;
    IF b IS NULL AND NEW.view_id IS NOT NULL THEN
      SELECT board_id, workspace_id INTO b, ws FROM _sb_board_ws_from_view(NEW.view_id);
    ELSE
      ws := _sb_ws_from_board(b);
    END IF;
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('message', NEW.id, ws, b, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    b := OLD.board_id;
    IF b IS NULL AND OLD.view_id IS NOT NULL THEN
      SELECT board_id, workspace_id INTO b, ws FROM _sb_board_ws_from_view(OLD.view_id);
    ELSE
      ws := _sb_ws_from_board(b);
    END IF;
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('message', OLD.id, ws, b, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_messages_outbox ON messages;
CREATE TRIGGER trg_messages_outbox
AFTER INSERT OR UPDATE OR DELETE ON messages
FOR EACH ROW EXECUTE FUNCTION emit_message_outbox();

-- INBOX MESSAGES (direct)
CREATE OR REPLACE FUNCTION emit_inbox_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('inbox', NEW.id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('inbox', OLD.id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_inbox_outbox ON inbox_messages;
CREATE TRIGGER trg_inbox_outbox
AFTER INSERT OR UPDATE OR DELETE ON inbox_messages
FOR EACH ROW EXECUTE FUNCTION emit_inbox_outbox();

-- ATTACHMENTS OUTBOX EMITTER
CREATE OR REPLACE FUNCTION emit_attachment_outbox()
RETURNS trigger AS $$
DECLARE
  b uuid;
  ws uuid;
BEGIN
  -- resolve board_id if only card_id is set
  b := COALESCE(NEW.board_id, OLD.board_id);

  IF b IS NULL AND (TG_OP <> 'DELETE') AND NEW.card_id IS NOT NULL THEN
    SELECT board_id INTO b FROM cards WHERE id = NEW.card_id;
  ELSIF b IS NULL AND (TG_OP = 'DELETE') AND OLD.card_id IS NOT NULL THEN
    SELECT board_id INTO b FROM cards WHERE id = OLD.card_id;
  END IF;

  -- resolve workspace
  IF b IS NOT NULL THEN
    ws := _sb_ws_from_board(b);
  END IF;

  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('attachment', NEW.id, ws, b, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('attachment', OLD.id, ws, b, 'delete', to_jsonb(OLD));
  END IF;

  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_attachments_outbox ON attachments;
CREATE TRIGGER trg_attachments_outbox
AFTER INSERT OR UPDATE OR DELETE ON attachments
FOR EACH ROW EXECUTE FUNCTION emit_attachment_outbox();


-- MENTIONS
CREATE OR REPLACE FUNCTION emit_mention_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('mention', NEW.id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('mention', OLD.id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_mentions_outbox ON mentions;
CREATE TRIGGER trg_mentions_outbox
AFTER INSERT OR UPDATE OR DELETE ON mentions
FOR EACH ROW EXECUTE FUNCTION emit_mention_outbox();

-- NOTIFICATIONS
CREATE OR REPLACE FUNCTION emit_notification_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('notification', NEW.id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('notification', OLD.id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_notifications_outbox ON notifications;
CREATE TRIGGER trg_notifications_outbox
AFTER INSERT OR UPDATE OR DELETE ON notifications
FOR EACH ROW EXECUTE FUNCTION emit_notification_outbox();

-- USER CONTACTS
CREATE OR REPLACE FUNCTION emit_user_contact_outbox()
RETURNS trigger AS $$
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('user_contact', NEW.user_id, 'upsert', to_jsonb(NEW));
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, op, payload)
    VALUES ('user_contact', OLD.user_id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_user_contacts_outbox ON user_contacts;
CREATE TRIGGER trg_user_contacts_outbox
AFTER INSERT OR UPDATE OR DELETE ON user_contacts
FOR EACH ROW EXECUTE FUNCTION emit_user_contact_outbox();


-- INVITES
CREATE OR REPLACE FUNCTION emit_invite_outbox() 
RETURNS trigger AS $$
DECLARE 
  payload jsonb;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    payload := jsonb_build_object(
      'id',            NEW.id,
      'email',         NEW.email,
      'scope_type',    NEW.scope_type,
      'status',        NEW.status,
      'workspace_id',  NEW.workspace_id,
      'board_id',      NEW.board_id,
      'contact_id',    NEW.contact_id,
      'target_role',   NEW.target_role,
      'board_role',    NEW.board_role,
      'created_at',    NEW.created_at,
      'expires_at',    NEW.expires_at,
      'accepted_at',   NEW.accepted_at,
      'accepted_by',   NEW.accepted_by,
      'revoked_at',    NEW.revoked_at
    );

    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES (
      'invite',
      NEW.id,
      NEW.workspace_id,
      NEW.board_id,
      'upsert',
      payload
    );
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES (
      'invite',
      OLD.id,
      OLD.workspace_id,
      OLD.board_id,
      'delete',
      to_jsonb(OLD)
    );
  END IF;

  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_invites_outbox ON invites;
CREATE TRIGGER trg_invites_outbox
AFTER INSERT OR UPDATE OR DELETE ON invites
FOR EACH ROW EXECUTE FUNCTION emit_invite_outbox();



-- ============================================================================
-- INVITE HELPERS (SERVER-SIDE CONVENIENCE)
-- ============================================================================

-- Create an invite row given a pre-hashed token (hash on app side or via sha256_hex)
CREATE OR REPLACE FUNCTION invite_create(
  p_sender       uuid,
  p_email        text,
  p_scope_type   invite_scope,
  p_workspace    uuid,
  p_board        uuid,
  p_contact      uuid,
  p_target_role  workspace_role,
  p_board_role   workspace_role,
  p_token_hash   text,
  p_expires_in   interval DEFAULT interval '7 days',
  p_note         text DEFAULT NULL
) RETURNS uuid AS $$
DECLARE 
  v_id uuid;
BEGIN
  INSERT INTO invites (
    id,
    sender_id,
    email,
    scope_type,
    workspace_id,
    board_id,
    contact_id,
    target_role,
    board_role,
    token_hash,
    expires_at,
    note
  )
  VALUES (
    gen_random_uuid(),
    p_sender,
    p_email,
    p_scope_type,
    p_workspace,
    p_board,
    p_contact,
    p_target_role,
    p_board_role,
    p_token_hash,
    now() + p_expires_in,
    p_note
  )
  RETURNING id INTO v_id;

  RETURN v_id;
END; $$ LANGUAGE plpgsql;


-- Accept an invite by token hash and upsert membership / contacts
CREATE OR REPLACE FUNCTION invite_accept(
  p_token_hash     text,
  p_accepting_user uuid
) RETURNS TABLE(
  invite_id    uuid,
  scope_type   invite_scope,
  workspace_id uuid,
  board_id     uuid,
  contact_id   uuid,
  target_role  workspace_role,
  board_role   workspace_role
) AS $$
DECLARE 
  v_inv invites;
BEGIN
  SELECT * INTO v_inv
  FROM invites
  WHERE token_hash = p_token_hash
    AND status = 'pending'
    AND now() < expires_at
  FOR UPDATE;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'invalid_or_expired_invite';
  END IF;

  -- Workspace membership
  IF v_inv.scope_type = 'workspace' AND v_inv.workspace_id IS NOT NULL THEN
    INSERT INTO workspace_members(workspace_id, user_id, role)
    VALUES (v_inv.workspace_id, p_accepting_user, COALESCE(v_inv.target_role, 'member'))
    ON CONFLICT (workspace_id, user_id) DO UPDATE
      SET role = GREATEST(EXCLUDED.role, workspace_members.role);
  END IF;

  -- Board membership
  IF v_inv.scope_type = 'board' AND v_inv.board_id IS NOT NULL THEN
    INSERT INTO board_members(board_id, user_id, role)
    VALUES (v_inv.board_id, p_accepting_user, COALESCE(v_inv.board_role, 'member'))
    ON CONFLICT (board_id, user_id) DO UPDATE
      SET role = GREATEST(EXCLUDED.role, board_members.role);
  END IF;

  -- Contact friendship reciprocity
  IF v_inv.scope_type = 'contact' THEN
    -- If the invite was created for a specific known user, use that
    -- Otherwise, treat sender and accepting user as contacts.
    -- We always create a symmetric friendship between sender and accepter.
    INSERT INTO user_contacts(user_id, contact_id, status, created_at, accepted_at)
    VALUES (v_inv.sender_id, p_accepting_user, 'accepted', now(), now())
    ON CONFLICT (user_id, contact_id) DO UPDATE
      SET status      = 'accepted',
          accepted_at = COALESCE(user_contacts.accepted_at, EXCLUDED.accepted_at);

    INSERT INTO user_contacts(user_id, contact_id, status, created_at, accepted_at)
    VALUES (p_accepting_user, v_inv.sender_id, 'accepted', now(), now())
    ON CONFLICT (user_id, contact_id) DO UPDATE
      SET status      = 'accepted',
          accepted_at = COALESCE(user_contacts.accepted_at, EXCLUDED.accepted_at);
  END IF;

  -- Mark invite as accepted
  UPDATE invites
     SET status      = 'accepted',
         accepted_at = now(),
         accepted_by = p_accepting_user
   WHERE id = v_inv.id;

  RETURN QUERY
  SELECT
    v_inv.id,
    v_inv.scope_type,
    v_inv.workspace_id,
    v_inv.board_id,
    v_inv.contact_id,
    v_inv.target_role,
    v_inv.board_role;
END; $$ LANGUAGE plpgsql;


-- Revoke a pending invite
CREATE OR REPLACE FUNCTION invite_revoke(p_token_hash text) 
RETURNS void AS $$
BEGIN
  UPDATE invites 
     SET status    = 'revoked',
         revoked_at = now()
   WHERE token_hash = p_token_hash
     AND status     = 'pending';
END; $$ LANGUAGE plpgsql;
