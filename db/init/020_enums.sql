
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
Do $$ BEGIN CREATE TYPE contact_status    AS ENUM ('pending','accepted','blocked');                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE invite_scope    AS ENUM ('workspace','board','contact');                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE entity_type    AS ENUM ('card','comment','message','doc','whiteboard');   EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- file token
DO $$ BEGIN CREATE TYPE file_token_audience AS ENUM (
  'download',
  'preview',
  'stream',
  'upload'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;


-- Worker jobs
DO $$ BEGIN CREATE TYPE worker_job_kind AS ENUM (
    'outbox_dispatch',
    'asset_variant',
    'invite_email',
    'mention_notify',
    'notification_push',
    'scheduled_reminder',
    'search_index',
    'cdn_garbage_collect',
    'analytics_aggregate',
    'cleanup'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE worker_job_status AS ENUM (
    'queued',
    'running',
    'done',
    'dead'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;


-- Push / Notifications
-- fcm     = Firebase Cloud Messaging (Android / Web / modern browsers)
-- apns    = Apple Push Notification Service (iOS / macOS Safari)
-- webpush = Standard Web Push protocol (service workers in browsers)
DO $$ BEGIN CREATE TYPE push_provider AS ENUM (
    'fcm',
    'apns',
    'webpush'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;


DO $$ BEGIN CREATE TYPE notification_channel AS ENUM (
    'in_app',
    'push',
    'email'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;


-- Sync
DO $$ BEGIN CREATE TYPE sync_scope_type AS ENUM (
    'workspace',
    'board',
    'inbox'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE outbox_topic AS ENUM (
  'user',
  'workspace',
  'workspace_member',
  'board',
  'board_member',
  'view',
  'card',
  'comment',
  'message',
  'attachment',
  'invite',
  'inbox',
  'mention',
  'notification',
  'user_contact'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN CREATE TYPE outbox_operation AS ENUM (
  'upsert',
  'delete'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;



-- Attachments
DO $$ BEGIN CREATE TYPE attachment_status AS ENUM (
  'pending',
  'processing',
  'ready',
  'failed',
  'deleted',
  'uploading'
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;


DO $$ BEGIN CREATE TYPE attachment_variant_type AS ENUM (
    'original',
    'thumb',          -- small preview for lists
    'preview',        -- medium preview (cards)
    'full',           -- resized display version (images/videos)
    'transcoded',     -- compressed/normalized media
    'poster',         -- video poster image
    'waveform',       -- audio visual
    'pdf_preview',    -- first page image
    'doc_preview',    -- office doc render
    'text_extract'    -- OCR / parsed content
); EXCEPTION WHEN duplicate_object THEN NULL; END $$;