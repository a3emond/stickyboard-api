-- ===========================================================
-- StickyBoard – PostgreSQL Schema (Upgraded for Collaboration)
-- File: 001_schema.sql
-- Date: 2025-10-24
-- ===========================================================

-- 1) Extensions ------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===========================================================
-- ENUM TYPES – COMPLETE SET (2025-10-24)
-- ===========================================================

-- User and system roles
DO $$ BEGIN
    CREATE TYPE user_role AS ENUM ('user', 'admin', 'moderator');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE board_role AS ENUM ('owner','editor','commenter','viewer');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE org_role AS ENUM ('owner','admin','moderator','member','guest');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Visibility & structure
DO $$ BEGIN
    CREATE TYPE board_visibility AS ENUM ('private','shared','public');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE tab_scope AS ENUM ('board','section');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Cards and links
DO $$ BEGIN
    CREATE TYPE card_type AS ENUM ('note','task','event','drawing');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE card_status AS ENUM ('open','in_progress','blocked','done','archived');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE link_type AS ENUM ('references','duplicate_of','relates_to','blocks','depends_on');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Clusters & activities
DO $$ BEGIN
    CREATE TYPE cluster_type AS ENUM ('manual','rule','similarity');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE activity_type AS ENUM (
        'card_created','card_updated','card_moved','comment_added',
        'status_changed','assignee_changed','link_added','link_removed',
        'rule_triggered','board_changed','cluster_changed','rule_changed'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE entity_type AS ENUM (
        'user','board','section','tab','card','link','cluster','rule','file'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Worker system
DO $$ BEGIN
    CREATE TYPE job_kind AS ENUM (
        'RuleExecutor','ClusterRebuilder','SearchIndexer',
        'SyncCompactor','NotificationDispatcher','AnalyticsExporter','Generic'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE job_status AS ENUM ('pending','running','completed','failed');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Messaging & social
DO $$ BEGIN
    CREATE TYPE message_type AS ENUM ('invite','system','direct','org_invite');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE relation_status AS ENUM ('pending','accepted','blocked');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- 3) Utility Functions ----------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 4) Core Identity --------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email        TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    avatar_uri   TEXT,
    prefs        JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_users_updated
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS auth_users (
    user_id       UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    password_hash TEXT NOT NULL,
    role          user_role NOT NULL DEFAULT 'user',
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_auth_users_updated
BEFORE UPDATE ON auth_users
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Refresh tokens (hashed, one-time, rotatable)
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash  TEXT NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    revoked     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_valid ON refresh_tokens(user_id, revoked, expires_at);

CREATE TRIGGER trg_refresh_tokens_updated
BEFORE UPDATE ON refresh_tokens
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- 5) Organizations & Memberships -------------------------------
CREATE TABLE IF NOT EXISTS organizations (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name       TEXT NOT NULL,
    owner_id   UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_orgs_updated
BEFORE UPDATE ON organizations
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS organization_members (
    org_id    UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    user_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role      org_role NOT NULL DEFAULT 'member',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (org_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_org_members_user ON organization_members(user_id);

-- 6) Boards & Structure ---------------------------------------
CREATE TABLE IF NOT EXISTS boards (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    owner_id         UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    org_id           UUID REFERENCES organizations(id) ON DELETE SET NULL,
    parent_board_id  UUID REFERENCES boards(id) ON DELETE CASCADE, -- personal sub-board parent
    title            TEXT NOT NULL,
    visibility       board_visibility NOT NULL DEFAULT 'private',
    theme            JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_boards_owner ON boards(owner_id);
CREATE INDEX IF NOT EXISTS ix_boards_org ON boards(org_id);
CREATE INDEX IF NOT EXISTS ix_boards_parent ON boards(parent_board_id);

CREATE TRIGGER trg_boards_updated
BEFORE UPDATE ON boards
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Board-level permissions (collaboration)
CREATE TABLE IF NOT EXISTS permissions (
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    board_id   UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    role       TEXT NOT NULL CHECK (role IN ('owner','editor','commenter','viewer')),
    granted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, board_id)
);

CREATE INDEX IF NOT EXISTS ix_permissions_board ON permissions(board_id);
CREATE INDEX IF NOT EXISTS ix_permissions_user ON permissions(user_id);

-- Sections
CREATE TABLE IF NOT EXISTS sections (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    board_id    UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    title       TEXT NOT NULL,
    position    INT NOT NULL DEFAULT 0,
    layout_meta JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_sections_board_pos ON sections(board_id, position);

CREATE TRIGGER trg_sections_updated
BEFORE UPDATE ON sections
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Tabs (can belong to a section or directly to the board via scope)
CREATE TABLE IF NOT EXISTS tabs (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    scope         tab_scope NOT NULL, -- 'board' | 'section'
    board_id      UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    section_id    UUID REFERENCES sections(id) ON DELETE SET NULL,
    title         TEXT NOT NULL,
    tab_type      TEXT NOT NULL,
    layout_config JSONB NOT NULL DEFAULT '{}'::jsonb,
    position      INT NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CHECK ((scope = 'board' AND section_id IS NULL) OR (scope = 'section'))
);

CREATE INDEX IF NOT EXISTS ix_tabs_board_scope ON tabs(board_id, scope, position);
CREATE INDEX IF NOT EXISTS ix_tabs_section_pos ON tabs(section_id, position);

CREATE TRIGGER trg_tabs_updated
BEFORE UPDATE ON tabs
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Cards (optional: belong to tab; section_id kept for fast filtering)
CREATE TABLE IF NOT EXISTS cards (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    board_id    UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    section_id  UUID REFERENCES sections(id) ON DELETE SET NULL,
    tab_id      UUID REFERENCES tabs(id) ON DELETE SET NULL,
    type        TEXT NOT NULL DEFAULT 'note',
    title       TEXT NOT NULL,
    content     JSONB NOT NULL DEFAULT '{}'::jsonb,
    status      TEXT,
    priority    INT,
    assignee_id UUID REFERENCES users(id) ON DELETE SET NULL,
    created_by  UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_cards_board ON cards(board_id);
CREATE INDEX IF NOT EXISTS ix_cards_section ON cards(section_id);
CREATE INDEX IF NOT EXISTS ix_cards_tab ON cards(tab_id);
CREATE INDEX IF NOT EXISTS ix_cards_status ON cards(status);
CREATE INDEX IF NOT EXISTS ix_cards_assignee ON cards(assignee_id);

CREATE TRIGGER trg_cards_updated
BEFORE UPDATE ON cards
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- 7) Tags & Links ----------------------------------------------
CREATE TABLE IF NOT EXISTS tags (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name       TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_tags_updated
BEFORE UPDATE ON tags
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS card_tags (
    card_id UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    tag_id  UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (card_id, tag_id)
);

CREATE TABLE IF NOT EXISTS links (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    from_card  UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    to_card    UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    rel_type   TEXT NOT NULL,
    created_by UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_links_from ON links(from_card);
CREATE INDEX IF NOT EXISTS ix_links_to ON links(to_card);

CREATE TRIGGER trg_links_updated
BEFORE UPDATE ON links
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- 8) Rules, Clusters, Activities -------------------------------
CREATE TABLE IF NOT EXISTS rules (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    board_id   UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    definition JSONB NOT NULL,
    enabled    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_rules_board ON rules(board_id);
CREATE INDEX IF NOT EXISTS ix_rules_enabled ON rules(enabled);

CREATE TRIGGER trg_rules_updated
BEFORE UPDATE ON rules
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS clusters (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    board_id    UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    cluster_type TEXT NOT NULL,
    rule_def    JSONB,
    visual_meta JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_clusters_board ON clusters(board_id);

CREATE TRIGGER trg_clusters_updated
BEFORE UPDATE ON clusters
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS cluster_members (
    cluster_id UUID NOT NULL REFERENCES clusters(id) ON DELETE CASCADE,
    card_id    UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    PRIMARY KEY (cluster_id, card_id)
);

CREATE TABLE IF NOT EXISTS activities (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    board_id   UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    card_id    UUID REFERENCES cards(id) ON DELETE SET NULL,
    actor_id   UUID REFERENCES users(id) ON DELETE SET NULL,
    act_type   TEXT NOT NULL,
    payload    JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_activities_board ON activities(board_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_activities_card ON activities(card_id, created_at DESC);

-- 9) Files & Operations ----------------------------------------
CREATE TABLE IF NOT EXISTS files (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    owner_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    board_id    UUID REFERENCES boards(id) ON DELETE SET NULL,
    card_id     UUID REFERENCES cards(id) ON DELETE SET NULL,
    storage_key TEXT NOT NULL UNIQUE,
    filename    TEXT NOT NULL,
    mime_type   TEXT,
    size_bytes  BIGINT NOT NULL DEFAULT 0,
    meta        JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_files_owner ON files(owner_id);
CREATE INDEX IF NOT EXISTS ix_files_board ON files(board_id);
CREATE INDEX IF NOT EXISTS ix_files_card ON files(card_id);

CREATE TRIGGER trg_files_updated
BEFORE UPDATE ON files
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TABLE IF NOT EXISTS operations (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    device_id    UUID NOT NULL,
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    entity       TEXT NOT NULL,
    entity_id    UUID NOT NULL,
    op_type      TEXT NOT NULL,
    payload      JSONB NOT NULL,
    version_prev INT,
    version_next INT,
    processed    BOOLEAN NOT NULL DEFAULT FALSE,
    processed_at TIMESTAMPTZ,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ops_user ON operations(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_device ON operations(device_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ops_pending ON operations(processed, created_at);

-- 10) Worker Jobs ----------------------------------------------
CREATE TABLE IF NOT EXISTS worker_jobs (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    job_kind     TEXT NOT NULL,
    priority     INT NOT NULL DEFAULT 0,
    run_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    status       job_status NOT NULL DEFAULT 'pending',
    attempt      INT NOT NULL DEFAULT 0,
    claimed_by   TEXT,
    claimed_at   TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT,
    payload      JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_jobs_ready ON worker_jobs(status, run_at);
CREATE INDEX IF NOT EXISTS ix_jobs_priority ON worker_jobs(priority DESC, run_at);

CREATE TRIGGER trg_jobs_updated
BEFORE UPDATE ON worker_jobs
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- 11) Social Graph (Friends) -----------------------------------
CREATE TABLE IF NOT EXISTS user_relations (
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    friend_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status      TEXT NOT NULL CHECK (status IN ('pending','accepted','blocked')),
    requested_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    accepted_at TIMESTAMPTZ,
    PRIMARY KEY (user_id, friend_id)
);

CREATE INDEX IF NOT EXISTS ix_user_relations_friend ON user_relations(friend_id);

-- 12) Messaging & Invites --------------------------------------
CREATE TABLE IF NOT EXISTS messages (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sender_id     UUID REFERENCES users(id) ON DELETE SET NULL,
    receiver_id   UUID REFERENCES users(id) ON DELETE CASCADE,
    subject       TEXT,
    body          TEXT,
    type          TEXT NOT NULL CHECK (type IN ('invite','system','direct','org_invite')),
    related_board UUID REFERENCES boards(id) ON DELETE CASCADE,
    related_org   UUID REFERENCES organizations(id) ON DELETE CASCADE,
    status        TEXT NOT NULL DEFAULT 'unread' CHECK (status IN ('unread','read','archived')),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_messages_receiver ON messages(receiver_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS invites (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sender_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    email       TEXT NOT NULL,
    board_id    UUID REFERENCES boards(id) ON DELETE CASCADE,
    org_id      UUID REFERENCES organizations(id) ON DELETE CASCADE,
    role        TEXT,
    token       TEXT NOT NULL UNIQUE,
    accepted    BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at  TIMESTAMPTZ NOT NULL DEFAULT (now() + interval '7 days')
);

CREATE INDEX IF NOT EXISTS ix_invites_email ON invites(email);
CREATE INDEX IF NOT EXISTS ix_invites_board ON invites(board_id);
CREATE INDEX IF NOT EXISTS ix_invites_org ON invites(org_id);

-- ===========================================================
-- End of schema
-- ===========================================================
