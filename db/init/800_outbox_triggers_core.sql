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
CREATE OR REPLACE FUNCTION emit_invite_outbox() RETURNS trigger AS $$
DECLARE payload jsonb;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    payload := jsonb_build_object(
      'id', NEW.id,
      'email', NEW.email,
      'status', NEW.status,
      'workspace_id', NEW.workspace_id,
      'board_id', NEW.board_id,
      'target_role', NEW.target_role,
      'board_role', NEW.board_role,
      'created_at', NEW.created_at,
      'expires_at', NEW.expires_at,
      'accepted_at', NEW.accepted_at,
      'accepted_by', NEW.accepted_by
    );
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('invite', NEW.id, NEW.workspace_id, NEW.board_id, 'upsert', payload);
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO event_outbox(topic, entity_id, workspace_id, board_id, op, payload)
    VALUES ('invite', OLD.id, OLD.workspace_id, OLD.board_id, 'delete', to_jsonb(OLD));
  END IF;
  RETURN COALESCE(NEW, OLD);
END; $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_invites_outbox ON invites;
CREATE TRIGGER trg_invites_outbox
AFTER INSERT OR UPDATE OR DELETE ON invites
FOR EACH ROW EXECUTE FUNCTION emit_invite_outbox();
