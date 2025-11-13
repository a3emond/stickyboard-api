
-- ============================================================================
-- ENUM TYPES
-- ============================================================================
DO $$ BEGIN CREATE TYPE user_role         AS ENUM ('user','admin','moderator');                    EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE workspace_role    AS ENUM ('owner','admin','moderator','member','guest');              EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE view_type         AS ENUM ('kanban','list','calendar','timeline','metrics','doc','whiteboard','chat'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE card_status       AS ENUM ('open','in_progress','blocked','done','archived'); EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE message_channel   AS ENUM ('board','view','direct','system');              EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE notification_type AS ENUM ('mention','reply','assignment','system');       EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE TYPE invite_status     AS ENUM ('pending','accepted','revoked','expired');      EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE contact_status    AS ENUM ('pending','accepted','blocked')                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;
Do $$ BEGIN CREATE TYPE invite_scope    AS ENUM ('workspace','board','contact')                 EXCEPTION WHEN duplicate_object THEN NULL; END $$;