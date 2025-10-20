-- ===========================================================
-- StickyBoard – PostgreSQL Schema
-- Final Version (2025-10-19)
-- ===========================================================

-- ===========================================================
-- Extensions
-- ===========================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===========================================================
-- Utility trigger function
-- ===========================================================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ===========================================================
-- 2.0 User Roles (Global)
-- ===========================================================
CREATE TYPE user_role AS ENUM ('user', 'admin', 'moderator');

-- ===========================================================
-- 2.1 Users
-- ===========================================================
CREATE TABLE users (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email          text NOT NULL UNIQUE,
  display_name   text NOT NULL,
  avatar_uri     text,
  prefs          jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at     timestamptz NOT NULL DEFAULT now(),
  updated_at     timestamptz NOT NULL DEFAULT now()
);

-- Case-insensitive uniqueness & trigram search
CREATE UNIQUE INDEX idx_users_email_lower ON users (lower(email));
CREATE INDEX idx_users_email_trgm ON users USING gin (email gin_trgm_ops);

CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.2 Authentication (API only)
-- ===========================================================
CREATE TABLE auth_users (
    user_id       uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    password_hash text NOT NULL,
    role          user_role NOT NULL DEFAULT 'user',
    last_login    timestamptz DEFAULT now(),
    created_at    timestamptz DEFAULT now(),
    updated_at    timestamptz DEFAULT now()
);

CREATE TRIGGER trg_auth_users_updated_at
BEFORE UPDATE ON auth_users
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.3 Boards
-- ===========================================================
CREATE TYPE board_visibility AS ENUM ('private','shared','public');

CREATE TABLE boards (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id      uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  title         text NOT NULL,
  visibility    board_visibility NOT NULL DEFAULT 'private',
  theme         jsonb NOT NULL DEFAULT '{}'::jsonb,
  rules         jsonb NOT NULL DEFAULT '[]'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_boards_owner ON boards(owner_id);

CREATE TRIGGER trg_boards_updated_at
BEFORE UPDATE ON boards
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.4 Permissions
-- ===========================================================
CREATE TYPE board_role AS ENUM ('owner','editor','commenter','viewer');

CREATE TABLE permissions (
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  role       board_role NOT NULL,
  granted_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, board_id)
);

CREATE INDEX idx_permissions_board ON permissions(board_id);

-- ===========================================================
-- 2.5 Sections
-- ===========================================================
CREATE TABLE sections (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id     uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title        text NOT NULL,
  position     integer NOT NULL,
  layout_meta  jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_sections_board_pos ON sections(board_id, position);

CREATE TRIGGER trg_sections_updated_at
BEFORE UPDATE ON sections
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.6 Tabs
-- ===========================================================
CREATE TYPE tab_scope AS ENUM ('board','section');

CREATE TABLE tabs (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  scope         tab_scope NOT NULL,
  board_id      uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  section_id    uuid REFERENCES sections(id) ON DELETE SET NULL,
  title         text NOT NULL,
  tab_type      text NOT NULL DEFAULT 'custom',
  layout_config jsonb NOT NULL DEFAULT '{}'::jsonb,
  position      integer NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT tabs_scope_consistency
    CHECK ((scope='board' AND section_id IS NULL)
        OR (scope='section' AND section_id IS NOT NULL))
);

CREATE INDEX idx_tabs_board_pos ON tabs(board_id, position);

CREATE TRIGGER trg_tabs_updated_at
BEFORE UPDATE ON tabs
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.7 Cards
-- ===========================================================
CREATE TYPE card_type AS ENUM ('note','task','event','drawing');
CREATE TYPE card_status AS ENUM ('open','in_progress','blocked','done','archived');

CREATE TABLE cards (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id     uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  section_id   uuid REFERENCES sections(id) ON DELETE SET NULL,
  tab_id       uuid REFERENCES tabs(id) ON DELETE SET NULL,
  type         card_type NOT NULL,
  title        text,
  content      jsonb NOT NULL DEFAULT '{}'::jsonb,
  ink_data     jsonb,
  due_date     timestamptz,
  start_time   timestamptz,
  end_time     timestamptz,
  priority     integer,
  status       card_status NOT NULL DEFAULT 'open',
  created_by   uuid REFERENCES users(id) ON DELETE SET NULL,
  assignee_id  uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  version      integer NOT NULL DEFAULT 0
);

CREATE INDEX idx_cards_board ON cards(board_id);
CREATE INDEX idx_cards_section ON cards(section_id);
CREATE INDEX idx_cards_due ON cards(due_date);
CREATE INDEX idx_cards_status ON cards(status);

CREATE TRIGGER trg_cards_updated_at
BEFORE UPDATE ON cards
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- Card tags
CREATE TABLE tags (
  id    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name  text NOT NULL UNIQUE
);

CREATE TABLE card_tags (
  card_id uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  tag_id  uuid NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
  PRIMARY KEY (card_id, tag_id)
);

CREATE INDEX idx_card_tags_tag ON card_tags(tag_id);

-- Card assignees (multi)
CREATE TABLE card_assignees (
  card_id uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  PRIMARY KEY (card_id, user_id)
);

-- ===========================================================
-- 2.8 Links
-- ===========================================================
CREATE TYPE link_type AS ENUM ('references','duplicate_of','relates_to','blocks','depends_on');

CREATE TABLE links (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  from_card  uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  to_card    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  rel_type   link_type NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  created_by uuid REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX idx_links_from ON links(from_card);
CREATE INDEX idx_links_to ON links(to_card);

-- ===========================================================
-- 2.9 Clusters
-- ===========================================================
CREATE TYPE cluster_type AS ENUM ('manual','rule','similarity');

CREATE TABLE clusters (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id     uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  cluster_type cluster_type NOT NULL,
  rule_def     jsonb,
  visual_meta  jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE cluster_members (
  cluster_id uuid NOT NULL REFERENCES clusters(id) ON DELETE CASCADE,
  card_id    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  PRIMARY KEY (cluster_id, card_id)
);

CREATE INDEX idx_cluster_members_card ON cluster_members(card_id);

-- ===========================================================
-- 2.10 Activities
-- ===========================================================
CREATE TYPE activity_type AS ENUM (
  'card_created','card_updated','card_moved','comment_added',
  'status_changed','assignee_changed','link_added',
  'link_removed','rule_triggered'
);

CREATE TABLE activities (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  card_id    uuid REFERENCES cards(id) ON DELETE SET NULL,
  actor_id   uuid REFERENCES users(id) ON DELETE SET NULL,
  act_type   activity_type NOT NULL,
  payload    jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_activities_board_time
  ON activities(board_id, created_at DESC);

-- ===========================================================
-- 2.11 Rules
-- ===========================================================
CREATE TABLE rules (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  definition jsonb NOT NULL,
  enabled    boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_rules_board ON rules(board_id);

CREATE TRIGGER trg_rules_updated_at
BEFORE UPDATE ON rules
FOR EACH ROW
EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 2.12 Files (Attachments)
-- ===========================================================
CREATE TABLE files (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  board_id    uuid REFERENCES boards(id) ON DELETE SET NULL,
  card_id     uuid REFERENCES cards(id) ON DELETE SET NULL,
  storage_key text NOT NULL,
  filename    text NOT NULL,
  mime_type   text,
  size_bytes  bigint,
  meta        jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_files_card ON files(card_id);

-- ===========================================================
-- 2.13 Operations (Sync Log)
-- ===========================================================
CREATE TYPE entity_type AS ENUM(
  'user','board','section','tab','card','link','cluster','rule','file'
);

CREATE TABLE operations (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  device_id     text NOT NULL,
  user_id       uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  entity        entity_type NOT NULL,
  entity_id     uuid NOT NULL,
  op_type       text NOT NULL,
  payload       jsonb NOT NULL,
  version_prev  integer,
  version_next  integer,
  created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_operations_entity_time
  ON operations(entity, created_at);

CREATE INDEX idx_operations_user_time
  ON operations(user_id, created_at);

-- ===========================================================
-- 5. Search / FTS
-- ===========================================================
ALTER TABLE cards ADD COLUMN tsv tsvector
  GENERATED ALWAYS AS (
    setweight(to_tsvector('simple', coalesce(title,'')), 'A') ||
    setweight(to_tsvector('simple', coalesce((content->>'recognizedText'),'')), 'B')
  ) STORED;

CREATE INDEX idx_cards_tsv ON cards USING gin(tsv);

-- ===========================================================
-- Done
-- ===========================================================
