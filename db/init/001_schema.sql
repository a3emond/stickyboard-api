-- ===========================================================
-- StickyBoard – PostgreSQL Schema with Worker Queue
-- Final Version: 2025-10-20
-- ===========================================================
-- This schema defines all tables, triggers, and functions for the
-- StickyBoard API and background worker system.
-- The API handles core CRUD and sync operations, while workers
-- process asynchronous tasks like rule evaluation, clustering,
-- indexing, and notifications.
-- ===========================================================

-- ===========================================================
-- 1. Extensions
-- ===========================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===========================================================
-- 2. Utility Functions
-- ===========================================================
-- Generic trigger to maintain updated_at timestamps.
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ===========================================================
-- 3. Enumerated Types
-- ===========================================================
CREATE TYPE user_role AS ENUM ('user', 'admin', 'moderator');
CREATE TYPE board_visibility AS ENUM ('private', 'shared', 'public');
CREATE TYPE board_role AS ENUM ('owner', 'editor', 'commenter', 'viewer');
CREATE TYPE tab_scope AS ENUM ('board', 'section');
CREATE TYPE card_type AS ENUM ('note', 'task', 'event', 'drawing');
CREATE TYPE card_status AS ENUM ('open', 'in_progress', 'blocked', 'done', 'archived');
CREATE TYPE link_type AS ENUM ('references', 'duplicate_of', 'relates_to', 'blocks', 'depends_on');
CREATE TYPE cluster_type AS ENUM ('manual', 'rule', 'similarity');
CREATE TYPE activity_type AS ENUM (
    'card_created', 'card_updated', 'card_moved', 'comment_added',
    'status_changed', 'assignee_changed', 'link_added', 'link_removed',
    'rule_triggered', 'board_changed', 'cluster_changed', 'rule_changed'
);
CREATE TYPE entity_type AS ENUM ('user', 'board', 'section', 'tab', 'card', 'link', 'cluster', 'rule', 'file');

-- ===========================================================
-- 4. Users & Authentication
-- ===========================================================
CREATE TABLE users (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email         text NOT NULL UNIQUE,
    display_name  text NOT NULL,
    avatar_uri    text,
    prefs         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_users_email_lower ON users (lower(email));
CREATE INDEX idx_users_email_trgm ON users USING gin (email gin_trgm_ops);
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

CREATE TABLE auth_users (
    user_id      uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    password_hash text NOT NULL,
    role         user_role NOT NULL DEFAULT 'user',
    last_login   timestamptz DEFAULT now(),
    created_at   timestamptz DEFAULT now(),
    updated_at   timestamptz DEFAULT now()
);

CREATE TRIGGER trg_auth_users_updated_at
    BEFORE UPDATE ON auth_users
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 5. Boards, Permissions, and Sections
-- ===========================================================
CREATE TABLE boards (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title        text NOT NULL,
    visibility   board_visibility NOT NULL DEFAULT 'private',
    theme        jsonb NOT NULL DEFAULT '{}'::jsonb,
    rules        jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_boards_updated_at
    BEFORE UPDATE ON boards
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

CREATE TABLE permissions (
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    role        board_role NOT NULL,
    granted_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, board_id)
);

CREATE TABLE sections (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id     uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    title        text NOT NULL,
    position     integer NOT NULL,
    layout_meta  jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_sections_updated_at
    BEFORE UPDATE ON sections
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 6. Tabs and Cards
-- ===========================================================
CREATE TABLE tabs (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    scope          tab_scope NOT NULL,
    board_id       uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    section_id     uuid REFERENCES sections(id) ON DELETE SET NULL,
    title          text NOT NULL,
    tab_type       text NOT NULL DEFAULT 'custom',
    layout_config  jsonb NOT NULL DEFAULT '{}'::jsonb,
    position       integer NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    CHECK (
        (scope = 'board' AND section_id IS NULL)
        OR (scope = 'section' AND section_id IS NOT NULL)
    )
);

CREATE TABLE cards (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id      uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    section_id    uuid REFERENCES sections(id) ON DELETE SET NULL,
    tab_id        uuid REFERENCES tabs(id) ON DELETE SET NULL,
    type          card_type NOT NULL,
    title         text,
    content       jsonb NOT NULL DEFAULT '{}'::jsonb,
    ink_data      jsonb,
    due_date      timestamptz,
    start_time    timestamptz,
    end_time      timestamptz,
    priority      integer,
    status        card_status NOT NULL DEFAULT 'open',
    created_by    uuid REFERENCES users(id) ON DELETE SET NULL,
    assignee_id   uuid REFERENCES users(id) ON DELETE SET NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    version       integer NOT NULL DEFAULT 0
);

CREATE TRIGGER trg_cards_updated_at
    BEFORE UPDATE ON cards
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 7. Tags and Links
-- ===========================================================
CREATE TABLE tags (
    id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL UNIQUE
);

CREATE TABLE card_tags (
    card_id uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    tag_id  uuid NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (card_id, tag_id)
);

CREATE TABLE links (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    from_card   uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    to_card     uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    rel_type    link_type NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL
);

-- ===========================================================
-- 8. Clusters, Activities, and Rules
-- ===========================================================
CREATE TABLE clusters (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id      uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    cluster_type  cluster_type NOT NULL,
    rule_def      jsonb,
    visual_meta   jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE cluster_members (
    cluster_id uuid NOT NULL REFERENCES clusters(id) ON DELETE CASCADE,
    card_id    uuid NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    PRIMARY KEY (cluster_id, card_id)
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

CREATE TABLE rules (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id    uuid NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    definition  jsonb NOT NULL,
    enabled     boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_rules_updated_at
    BEFORE UPDATE ON rules
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

-- ===========================================================
-- 9. Files & Operations (Sync)
-- ===========================================================
CREATE TABLE files (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    board_id     uuid REFERENCES boards(id) ON DELETE SET NULL,
    card_id      uuid REFERENCES cards(id) ON DELETE SET NULL,
    storage_key  text NOT NULL,
    filename     text NOT NULL,
    mime_type    text,
    size_bytes   bigint,
    meta         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at   timestamptz NOT NULL DEFAULT now()
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

-- ===========================================================
-- 10. Worker Queue System
-- ===========================================================
CREATE TYPE job_kind AS ENUM (
    'RuleExecutor',
    'ClusterRebuilder',
    'SearchIndexer',
    'SyncCompactor',
    'NotificationDispatcher',
    'AnalyticsExporter',
    'Generic'
);

CREATE TYPE job_status AS ENUM ('queued', 'running', 'succeeded', 'failed', 'canceled', 'dead');

CREATE TABLE worker_jobs (
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

CREATE TRIGGER trg_worker_jobs_updated_at
    BEFORE UPDATE ON worker_jobs
    FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

CREATE INDEX idx_worker_jobs_ready
    ON worker_jobs (status, run_at, priority DESC, created_at)
    WHERE status = 'queued';

CREATE INDEX idx_worker_jobs_kind_status
    ON worker_jobs (job_kind, status);

CREATE UNIQUE INDEX idx_worker_jobs_dedupe
    ON worker_jobs (job_kind, dedupe_key)
    WHERE dedupe_key IS NOT NULL AND status IN ('queued', 'running');

CREATE INDEX idx_worker_jobs_payload_gin
    ON worker_jobs USING gin (payload jsonb_path_ops);

CREATE TABLE worker_job_attempts (
    id           bigserial PRIMARY KEY,
    job_id       uuid NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
    started_at   timestamptz NOT NULL DEFAULT now(),
    finished_at  timestamptz,
    ok           boolean,
    error        text
);

CREATE TABLE worker_job_deadletters (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_id      uuid UNIQUE,
    job_kind    job_kind NOT NULL,
    payload     jsonb NOT NULL,
    attempts    integer NOT NULL,
    last_error  text,
    dead_at     timestamptz NOT NULL DEFAULT now()
);

-- ===========================================================
-- 11. Event → Queue Bridge
-- ===========================================================
CREATE OR REPLACE FUNCTION enqueue_jobs_on_activity()
RETURNS trigger AS $$
DECLARE
    v_kind    job_kind;
    v_payload jsonb;
BEGIN
    IF NEW.act_type IN ('card_created', 'card_updated', 'status_changed', 'assignee_changed', 'link_added', 'link_removed') THEN
        v_kind := 'RuleExecutor';
    ELSIF NEW.act_type IN ('rule_changed', 'board_changed', 'cluster_changed') THEN
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

CREATE TRIGGER trg_activities_enqueue_jobs
    AFTER INSERT ON activities
    FOR EACH ROW
    EXECUTE PROCEDURE enqueue_jobs_on_activity();

-- ===========================================================
-- 12. Search / Full Text Indexing
-- ===========================================================
ALTER TABLE cards
ADD COLUMN IF NOT EXISTS tsv tsvector GENERATED ALWAYS AS (
    setweight(to_tsvector('simple', coalesce(title, '')), 'A') ||
    setweight(to_tsvector('simple', coalesce((content ->> 'recognizedText'), '')), 'B')
) STORED;

CREATE INDEX idx_cards_tsv ON cards USING gin (tsv);

-- ===========================================================
-- End of Schema
-- ===========================================================
