-- ===========================================================
-- StickyBoard Core Edition – Collaborative Boards
-- Core Only: Auth, Boards, Org, Sections, Tabs, Cards, Comments, Chat
-- ===========================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Utility
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Enums
DO $$ BEGIN CREATE TYPE user_role AS ENUM ('user','admin','moderator'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE board_role AS ENUM ('owner','editor','commenter','viewer'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE org_role AS ENUM ('owner','admin','moderator','member','guest'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE board_visibility AS ENUM ('private_','shared','public_'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- NOTE: tab_scope removed (no tabs under sections)

DO $$ BEGIN
  CREATE TYPE tab_type AS ENUM (
    'board',       -- default kanban-like tab
    'calendar',    -- schedule / agenda view
    'timeline',    -- roadmap / chronological
    'kanban',      -- column workflow
    'whiteboard',  -- free-form canvas semantics
    'chat',        -- discussion tab
    'metrics',     -- dashboard / analytics
    'custom'       -- fallback / experimental
  );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE card_type AS ENUM ('note','task','event_','drawing'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE card_status AS ENUM ('open','in_progress','blocked','done','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE message_type AS ENUM ('invite','system','direct','org_invite'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE message_status AS ENUM ('unread','read','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE relation_status AS ENUM ('active_','blocked','inactive'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ===========================================================
-- Users & Auth
-- ===========================================================
CREATE TABLE IF NOT EXISTS users (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email         text NOT NULL UNIQUE,
  display_name  text NOT NULL,
  avatar_uri    text,
  prefs         jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  deleted_at    timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_email_lower ON users (lower(email));
DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS auth_users (
  user_id       uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  password_hash text NOT NULL,
  role          user_role NOT NULL DEFAULT 'user',
  last_login    timestamptz DEFAULT now(),
  created_at    timestamptz DEFAULT now(),
  updated_at    timestamptz DEFAULT now(),
  deleted_at    timestamptz
);
DROP TRIGGER IF EXISTS trg_auth_users_updated_at ON auth_users;
CREATE TRIGGER trg_auth_users_updated_at BEFORE UPDATE ON auth_users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS refresh_tokens (
  token_hash text PRIMARY KEY,
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  expires_at timestamptz NOT NULL DEFAULT (now() + interval '30 days'),
  created_at timestamptz NOT NULL DEFAULT now(),
  revoked    boolean NOT NULL DEFAULT false
);

-- ===========================================================
-- Organizations
-- ===========================================================
CREATE TABLE IF NOT EXISTS organizations (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name       text NOT NULL,
  owner_id   uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
DROP TRIGGER IF EXISTS trg_organizations_updated ON organizations;
CREATE TRIGGER trg_organizations_updated BEFORE UPDATE ON organizations FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS organization_members (
  org_id    uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  user_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  role      org_role NOT NULL DEFAULT 'member',
  joined_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (org_id, user_id)
);

-- ===========================================================
-- Boards
-- ===========================================================
CREATE TABLE IF NOT EXISTS board_folders (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id     uuid REFERENCES organizations(id) ON DELETE CASCADE,
  user_id    uuid REFERENCES users(id) ON DELETE CASCADE,
  name       text NOT NULL,
  icon       text,                         -- optional UI icon (emoji or name)
  color      text,                         -- optional color tag (#hex or token)
  meta       jsonb NOT NULL DEFAULT '{}'::jsonb,   -- future-proofing metadata
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);

DROP TRIGGER IF EXISTS trg_board_folders_updated_at ON board_folders;
CREATE TRIGGER trg_board_folders_updated_at
BEFORE UPDATE ON board_folders
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS boards (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id        uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  org_id          uuid REFERENCES organizations(id) ON DELETE SET NULL,
  folder_id       uuid REFERENCES board_folders(id) ON DELETE SET NULL, 
  title           text NOT NULL,
  visibility      board_visibility NOT NULL DEFAULT 'private_',
  theme           jsonb NOT NULL DEFAULT '{}'::jsonb,
  meta            jsonb NOT NULL DEFAULT '{}'::jsonb, 
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz
);

DROP TRIGGER IF EXISTS trg_boards_updated_at ON boards;
CREATE TRIGGER trg_boards_updated_at
BEFORE UPDATE ON boards
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_boards_folder ON boards(folder_id);


CREATE TABLE IF NOT EXISTS permissions (
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  role       board_role NOT NULL,
  granted_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, board_id)
);

-- ===========================================================
-- Tabs (Board Views)
-- ===========================================================
CREATE TABLE IF NOT EXISTS tabs (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id      uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title         text NOT NULL,
  tab_type      tab_type NOT NULL DEFAULT 'board',
  layout_config jsonb NOT NULL DEFAULT '{}'::jsonb,
  position      integer NOT NULL DEFAULT 0,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  deleted_at    timestamptz
);
DROP TRIGGER IF EXISTS trg_tabs_updated_at ON tabs;
CREATE TRIGGER trg_tabs_updated_at BEFORE UPDATE ON tabs FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_tabs_board_pos ON tabs(board_id, position);

-- ===========================================================
-- Sections (Hierarchical Containers under Tabs)
-- ===========================================================
CREATE TABLE IF NOT EXISTS sections (
  id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tab_id             uuid NOT NULL REFERENCES tabs(id) ON DELETE CASCADE,
  parent_section_id  uuid REFERENCES sections(id) ON DELETE CASCADE,
  title              text NOT NULL,
  position           integer NOT NULL DEFAULT 0,
  layout_meta        jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now(),
  deleted_at         timestamptz
);
DROP TRIGGER IF EXISTS trg_sections_updated_at ON sections;
CREATE TRIGGER trg_sections_updated_at BEFORE UPDATE ON sections FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_sections_tab_parent_pos ON sections(tab_id, parent_section_id, position);

-- ===========================================================
-- Cards
-- ===========================================================
CREATE TABLE IF NOT EXISTS cards (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  tab_id      uuid NOT NULL REFERENCES tabs(id) ON DELETE CASCADE,
  section_id  uuid REFERENCES sections(id) ON DELETE SET NULL,
  type        card_type NOT NULL,
  title       text,
  content     jsonb NOT NULL DEFAULT '{}'::jsonb,
  ink_data    jsonb,
  due_date    timestamptz,
  start_time  timestamptz,
  end_time    timestamptz,
  priority    integer,
  status      card_status NOT NULL DEFAULT 'open',
  tags        text[] NOT NULL DEFAULT '{}',
  created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
  assignee_id uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  version     integer NOT NULL DEFAULT 0,
  deleted_at  timestamptz
);
DROP TRIGGER IF EXISTS trg_cards_updated_at ON cards;
CREATE TRIGGER trg_cards_updated_at BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_cards_board_tab_section ON cards(board_id, tab_id, section_id);
CREATE INDEX IF NOT EXISTS ix_cards_status_updated ON cards(status, updated_at);

-- ===========================================================
-- Card Comments
-- ===========================================================
CREATE TABLE IF NOT EXISTS card_comments (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id     uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE SET NULL,
  content     text NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  deleted_at  timestamptz
);
DROP TRIGGER IF EXISTS trg_card_comments_updated_at ON card_comments;
CREATE TRIGGER trg_card_comments_updated_at
  BEFORE UPDATE ON card_comments
  FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_card_comments_card ON card_comments(card_id, created_at);

-- ===========================================================
-- Board Chat Messages
-- ===========================================================
CREATE TABLE IF NOT EXISTS board_messages (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE SET NULL,
  content     text NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  deleted_at  timestamptz
);
DROP TRIGGER IF EXISTS trg_board_messages_updated_at ON board_messages;
CREATE TRIGGER trg_board_messages_updated_at
  BEFORE UPDATE ON board_messages
  FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS ix_board_messages_board ON board_messages(board_id, created_at);

-- ===========================================================
-- Social Graph, Messaging, Invites
-- ===========================================================
CREATE TABLE IF NOT EXISTS user_relations (
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  friend_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  status      relation_status NOT NULL DEFAULT 'active_', -- fixed to match enum
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  UNIQUE (user_id, friend_id)
);

CREATE TABLE IF NOT EXISTS messages (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id     uuid REFERENCES users(id) ON DELETE SET NULL,
  receiver_id   uuid REFERENCES users(id) ON DELETE CASCADE,
  subject       text,
  body          text,
  type          message_type NOT NULL,
  related_board uuid REFERENCES boards(id) ON DELETE CASCADE,
  related_org   uuid REFERENCES organizations(id) ON DELETE CASCADE,
  status        message_status NOT NULL DEFAULT 'unread',
  created_at    timestamptz NOT NULL DEFAULT now(),
  deleted_at    timestamptz
);

CREATE TABLE IF NOT EXISTS invites (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  email       text NOT NULL,
  board_id    uuid REFERENCES boards(id) ON DELETE CASCADE,
  org_id      uuid REFERENCES organizations(id) ON DELETE CASCADE,
  board_role  board_role,
  org_role    org_role,
  token       text NOT NULL UNIQUE,
  accepted    boolean NOT NULL DEFAULT false,
  created_at  timestamptz NOT NULL DEFAULT now(),
  expires_at  timestamptz NOT NULL DEFAULT (now() + interval '7 days'),
  CONSTRAINT invites_target_xor CHECK ((board_id IS NOT NULL)::int + (org_id IS NOT NULL)::int <= 1),
  CONSTRAINT invites_board_role_req CHECK (board_id IS NULL OR board_role IS NOT NULL),
  CONSTRAINT invites_org_role_req CHECK (org_id IS NULL OR org_role IS NOT NULL)
);
