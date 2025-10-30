-- ===========================================================
-- StickyBoard – PostgreSQL Schema (Refactored for Valid Syntax)
-- File: 001_schema.sql
-- Date: 2025-10-26
-- Notes:
--   * Fixed all invalid PostgreSQL trigger syntax (removed "CREATE TRIGGER IF NOT EXISTS").
--   * Standardized to DROP TRIGGER IF EXISTS …; CREATE TRIGGER … EXECUTE FUNCTION …
--   * Kept table/column shapes identical to your 2025-10-24 file.
--   * No behavioral changes beyond trigger creation syntax and EXECUTE FUNCTION usage.
-- ===========================================================

-- ===========================================================
-- 1) Extensions
-- ===========================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===========================================================
-- 2) Utility Functions
-- ===========================================================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ===========================================================
-- 3) Enumerated Types (guarded with DO/EXCEPTION to be idempotent)
-- ===========================================================

-- User & Authorization
DO $$ BEGIN CREATE TYPE user_role AS ENUM ('user','admin','moderator'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE board_role AS ENUM ('owner','editor','commenter','viewer'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE org_role AS ENUM ('owner','admin','moderator','member','guest'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Visibility & Structure
DO $$ BEGIN CREATE TYPE board_visibility AS ENUM ('private_','shared','public_'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE tab_scope AS ENUM ('board','section'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Cards, Links & Clusters
DO $$ BEGIN CREATE TYPE card_type AS ENUM ('note','task','event','drawing'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE card_status AS ENUM ('open','in_progress','blocked','done','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE link_type AS ENUM ('references','duplicate_of','relates_to','blocks','depends_on'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE cluster_type AS ENUM ('manual','rule','similarity'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Activity & Entity Types
DO $$ BEGIN CREATE TYPE activity_type AS ENUM (
    'card_created','card_updated','card_moved','comment_added',
    'status_changed','assignee_changed','link_added','link_removed',
    'rule_triggered','board_changed','cluster_changed','rule_changed'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE entity_type AS ENUM ('user','board','section','tab','card','link','cluster','rule','file'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Worker / Jobs (all lowercase)
DO $$ BEGIN CREATE TYPE job_kind AS ENUM (
    'ruleexecutor',
    'clusterrebuilder',
    'searchindexer',
    'synccompactor',
    'notificationdispatcher',
    'analyticsexporter',
    'generic'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE job_status AS ENUM (
    'queued','running','succeeded','failed','canceled','dead'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Messaging & Social
DO $$ BEGIN CREATE TYPE message_type AS ENUM ('invite','system','direct','org_invite'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE message_status AS ENUM ('unread','read','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE relation_status AS ENUM ('active','blocked','inactive'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;



-- ===========================================================
-- 4) Users & Authentication
-- ===========================================================
CREATE TABLE IF NOT EXISTS users (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email         text NOT NULL UNIQUE,
  display_name  text NOT NULL,
  avatar_uri    text,
  prefs         jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_email_lower ON users (lower(email));
CREATE INDEX IF NOT EXISTS idx_users_email_trgm ON users USING gin (email gin_trgm_ops);
DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS auth_users (
  user_id       uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  password_hash text NOT NULL,
  role          user_role NOT NULL DEFAULT 'user',
  last_login    timestamptz DEFAULT now(),
  created_at    timestamptz DEFAULT now(),
  updated_at    timestamptz DEFAULT now()
);
DROP TRIGGER IF EXISTS trg_auth_users_updated_at ON auth_users;
CREATE TRIGGER trg_auth_users_updated_at BEFORE UPDATE ON auth_users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS refresh_tokens (
  token_hash text PRIMARY KEY,
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  expires_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  revoked    boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);

-- ===========================================================
-- 5) Organizations & Memberships
-- ===========================================================
CREATE TABLE IF NOT EXISTS organizations (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name       text NOT NULL,
  owner_id   uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
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
CREATE INDEX IF NOT EXISTS ix_org_members_user ON organization_members(user_id);

-- ===========================================================
-- 6) Boards, Permissions, Sections
-- ===========================================================
CREATE TABLE IF NOT EXISTS boards (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id        uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  org_id          uuid REFERENCES organizations(id) ON DELETE SET NULL,
  parent_board_id uuid REFERENCES boards(id) ON DELETE CASCADE,
  title           text NOT NULL,
  visibility      board_visibility NOT NULL DEFAULT 'private_',
  theme           jsonb NOT NULL DEFAULT '{}'::jsonb,
  rules           jsonb NOT NULL DEFAULT '[]'::jsonb,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_boards_org ON boards(org_id);
CREATE INDEX IF NOT EXISTS ix_boards_parent ON boards(parent_board_id);
DROP TRIGGER IF EXISTS trg_boards_updated_at ON boards;
CREATE TRIGGER trg_boards_updated_at BEFORE UPDATE ON boards FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS permissions (
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  role       board_role NOT NULL,
  granted_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, board_id)
);

CREATE TABLE IF NOT EXISTS sections (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  title       text NOT NULL,
  position    integer NOT NULL,
  layout_meta jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now()
);
DROP TRIGGER IF EXISTS trg_sections_updated_at ON sections;
CREATE TRIGGER trg_sections_updated_at BEFORE UPDATE ON sections FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ===========================================================
-- 7) Tabs & Cards
-- ===========================================================
CREATE TABLE IF NOT EXISTS tabs (
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
  CHECK ((scope='board' AND section_id IS NULL) OR (scope='section' AND section_id IS NOT NULL))
);

CREATE TABLE IF NOT EXISTS cards (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  section_id  uuid REFERENCES sections(id) ON DELETE SET NULL,
  tab_id      uuid REFERENCES tabs(id) ON DELETE SET NULL,
  type        card_type NOT NULL,
  title       text,
  content     jsonb NOT NULL DEFAULT '{}'::jsonb,
  ink_data    jsonb,
  due_date    timestamptz,
  start_time  timestamptz,
  end_time    timestamptz,
  priority    integer,
  status      card_status NOT NULL DEFAULT 'open',
  created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
  assignee_id uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  version     integer NOT NULL DEFAULT 0
);
DROP TRIGGER IF EXISTS trg_cards_updated_at ON cards;
CREATE TRIGGER trg_cards_updated_at BEFORE UPDATE ON cards FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ===========================================================
-- 8) Tags & Links
-- ===========================================================
CREATE TABLE IF NOT EXISTS tags (
  id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS card_tags (
  card_id uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  tag_id  uuid NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
  PRIMARY KEY (card_id, tag_id)
);

CREATE TABLE IF NOT EXISTS links (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  from_card  uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  to_card    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  rel_type   link_type NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  created_by uuid REFERENCES users(id) ON DELETE SET NULL
);

-- ===========================================================
-- 9) Clusters, Cluster Members, Activities, Rules
-- ===========================================================
CREATE TABLE IF NOT EXISTS clusters (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id     uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  cluster_type cluster_type NOT NULL,
  rule_def     jsonb,
  visual_meta  jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS cluster_members (
  cluster_id uuid NOT NULL REFERENCES clusters(id) ON DELETE CASCADE,
  card_id    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
  PRIMARY KEY (cluster_id, card_id)
);

CREATE TABLE IF NOT EXISTS activities (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  card_id    uuid REFERENCES cards(id) ON DELETE SET NULL,
  actor_id   uuid REFERENCES users(id) ON DELETE SET NULL,
  act_type   activity_type NOT NULL,
  payload    jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS rules (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  board_id   uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
  definition jsonb NOT NULL,
  enabled    boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);
DROP TRIGGER IF EXISTS trg_rules_updated_at ON rules;
CREATE TRIGGER trg_rules_updated_at BEFORE UPDATE ON rules FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ===========================================================
-- 10) Files & Operations (Sync Log)
-- ===========================================================
CREATE TABLE IF NOT EXISTS files (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  board_id    uuid REFERENCES boards(id) ON DELETE SET NULL,
  card_id     uuid REFERENCES cards(id) ON DELETE SET NULL,
  storage_key text NOT NULL,
  filename    text NOT NULL,
  mime_type   text,
  size_bytes  bigint,
  meta        jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS operations (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  device_id    text NOT NULL,
  user_id      uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  entity       entity_type NOT NULL,
  entity_id    uuid NOT NULL,
  op_type      text NOT NULL,
  payload      jsonb NOT NULL,
  version_prev integer,
  version_next integer,
  created_at   timestamptz NOT NULL DEFAULT now(),
  processed    boolean NOT NULL DEFAULT false
);

-- ===========================================================
-- 11) Worker Queue System
-- ===========================================================
CREATE TABLE IF NOT EXISTS worker_jobs (
  id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  job_kind     job_kind NOT NULL,
  priority     smallint NOT NULL DEFAULT 0,
  run_at       timestamptz NOT NULL DEFAULT now(),
  max_attempts smallint NOT NULL DEFAULT 10,
  attempt      integer NOT NULL DEFAULT 0,
  dedupe_key   text,
  payload      jsonb NOT NULL DEFAULT '{}'::jsonb,
  status       job_status NOT NULL DEFAULT 'queued',
  claimed_by   text,
  claimed_at   timestamptz,
  last_error   text,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);
DROP TRIGGER IF EXISTS trg_worker_jobs_updated_at ON worker_jobs;
CREATE TRIGGER trg_worker_jobs_updated_at BEFORE UPDATE ON worker_jobs FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX IF NOT EXISTS idx_worker_jobs_ready ON worker_jobs (status, run_at, priority DESC, created_at) WHERE status = 'queued';
CREATE INDEX IF NOT EXISTS idx_worker_jobs_kind_status ON worker_jobs (job_kind, status);
CREATE UNIQUE INDEX IF NOT EXISTS idx_worker_jobs_dedupe ON worker_jobs (job_kind, dedupe_key) WHERE dedupe_key IS NOT NULL AND status IN ('queued','running');
CREATE INDEX IF NOT EXISTS idx_worker_jobs_payload_gin ON worker_jobs USING gin (payload jsonb_path_ops);

CREATE TABLE IF NOT EXISTS worker_job_attempts (
  id          bigserial PRIMARY KEY,
  job_id      uuid NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
  started_at  timestamptz NOT NULL DEFAULT now(),
  finished_at timestamptz,
  ok          boolean,
  error       text
);

CREATE TABLE IF NOT EXISTS worker_job_deadletters (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id     uuid UNIQUE,
  job_kind   job_kind NOT NULL,
  payload    jsonb NOT NULL,
  attempts   integer NOT NULL,
  last_error text,
  dead_at    timestamptz NOT NULL DEFAULT now()
);

-- Bridge function to enqueue follow-up jobs when activities are inserted
CREATE OR REPLACE FUNCTION enqueue_jobs_on_activity()
RETURNS trigger AS $$
DECLARE
  v_kind    job_kind;
  v_payload jsonb;
BEGIN
  IF NEW.act_type IN ('card_created','card_updated','status_changed','assignee_changed','link_added','link_removed') THEN
    v_kind := 'RuleExecutor';
  ELSIF NEW.act_type IN ('rule_changed','board_changed','cluster_changed') THEN
    v_kind := 'ClusterRebuilder';
  ELSE
    RETURN NEW;
  END IF;

  v_payload := jsonb_build_object(
    'activity_id', NEW.id,
    'board_id', NEW.board_id,
    'card_id', NEW.card_id,
    'act_type', NEW.act_type,
    'payload', NEW.payload
  );
  INSERT INTO worker_jobs (job_kind, payload) VALUES (v_kind, v_payload);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_activities_enqueue_jobs ON activities;
CREATE TRIGGER trg_activities_enqueue_jobs AFTER INSERT ON activities FOR EACH ROW EXECUTE FUNCTION enqueue_jobs_on_activity();

-- ===========================================================
-- 12) Search / Full Text Indexing on cards
-- ===========================================================
ALTER TABLE cards
  ADD COLUMN IF NOT EXISTS tsv tsvector GENERATED ALWAYS AS (
    setweight(to_tsvector('simple', coalesce(title, '')),'A') ||
    setweight(to_tsvector('simple', coalesce((content ->> 'recognizedText'), '')),'B')
  ) STORED;
CREATE INDEX IF NOT EXISTS idx_cards_tsv ON cards USING gin (tsv);

-- ===========================================================
-- 13) Social Graph, Messaging, Invites
-- ===========================================================
CREATE TABLE user_relations (
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    friend_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status       relation_status NOT NULL DEFAULT 'active',
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, friend_id)
);

CREATE INDEX ix_user_relations_user ON user_relations(user_id);

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
  created_at    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_messages_receiver ON messages(receiver_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS invites (
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id   uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  email       text NOT NULL,

  -- Exactly one of these may be set (friend invite = both NULL)
  board_id    uuid REFERENCES boards(id) ON DELETE CASCADE,
  org_id      uuid REFERENCES organizations(id) ON DELETE CASCADE,

  -- Domain-scoped roles (nullable; only one applies depending on target)
  board_role  board_role,
  org_role    org_role,

  token       text NOT NULL UNIQUE,
  accepted    boolean NOT NULL DEFAULT false,
  created_at  timestamptz NOT NULL DEFAULT now(),
  expires_at  timestamptz NOT NULL DEFAULT (now() + interval '7 days'),

  -- Enforce: board XOR org (friend invite when both NULL)
  CONSTRAINT invites_target_xor CHECK (
    (board_id IS NOT NULL)::int + (org_id IS NOT NULL)::int <= 1
  ),

  -- If board_id set, board_role must be set; if org_id set, org_role must be set
  CONSTRAINT invites_board_role_req CHECK (
    board_id IS NULL OR board_role IS NOT NULL
  ),
  CONSTRAINT invites_org_role_req CHECK (
    org_id IS NULL OR org_role IS NOT NULL
  )
);

-- indexes
CREATE INDEX IF NOT EXISTS ix_invites_email ON invites(email);
CREATE INDEX IF NOT EXISTS ix_invites_board ON invites(board_id);
CREATE INDEX IF NOT EXISTS ix_invites_org   ON invites(org_id);



-- ===========================================================
-- End of Schema
-- ===========================================================
