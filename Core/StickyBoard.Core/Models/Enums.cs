namespace StickyBoard.Core.Models;

public enum ErrorCode
{
    SERVER_ERROR,
    AUTH_INVALID,
    AUTH_EXPIRED,
    NOT_FOUND,
    FORBIDDEN,
    VALIDATION_ERROR,
    CONFLICT
}

// ============================
// Users & Auth
// ============================
public enum UserRole
{
    user,
    admin,
    moderator
}

// ============================
// Workspaces
// ============================
public enum WorkspaceRole
{
    owner,
    admin,
    moderator,
    member,
    guest,
    none // for users with no access in board permission override context
}

// ============================
// Views
// ============================
public enum ViewType
{
    kanban,
    list,
    calendar,
    timeline,
    metrics,
    doc,
    whiteboard,
    chat
}

// ============================
// EntityType
// ============================
public enum EntityType
{
    card,
    comment,
    message,
    doc,
    whiteboard
}

// ============================
// Cards
// ============================
public enum CardStatus
{
    open,
    in_progress,
    blocked,
    done,
    archived
}

// ============================
// Messaging & Social
// ============================

// SQL: message_channel
public enum MessageChannel
{
    board,
    view,
    direct,
    system
}

// SQL: notification_type
public enum NotificationType
{
    mention,
    reply,
    assignment,
    system
}

// SQL: invite_status
public enum InviteStatus
{
    pending,
    accepted,
    revoked,
    expired
}

// SQL: invite_scope
public enum InviteScope
{
    Workspace,
    Board,
    Contact
}

// SQL: contact_status
public enum ContactStatus
{
    pending,
    accepted,
    blocked
}

// =====================
// Worker Jobs
// =====================

public enum WorkerJobKind
{
    OutboxDispatch,
    AssetVariant,
    InviteEmail,
    MentionNotify,
    NotificationPush,
    ScheduledReminder,
    SearchIndex,
    CdnGarbageCollect,
    AnalyticsAggregate,
    Cleanup
}

public enum WorkerJobStatus
{
    Queued,
    Running,
    Done,
    Dead
}

// =====================
// Push / Notifications
// =====================

public enum PushProvider
{
    Fcm,
    Apns,
    WebPush
}

public enum NotificationChannel
{
    InApp,
    Push,
    Email
}

// =====================
// Sync
// =====================

public enum SyncScopeType
{
    Workspace,
    Board,
    Inbox
}

public enum OutboxTopic
{
    User,
    Workspace,
    WorkspaceMember,
    Board,
    BoardMember,
    View,
    Card,
    Comment,
    Message,
    Attachment,
    Invite,
    Inbox,
    Mention,
    Notification,
    UserContact
}

public enum OutboxOperation
{
    Upsert,
    Delete
}

// =====================
// Attachments
// =====================

public enum AttachmentStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
    Deleted,
    Uploading
}

public enum AttachmentVariantType
{
    Original,
    Thumb,
    Preview,
    Full,
    Transcoded,
    Poster,
    Waveform,
    PdfPreview,
    DocPreview,
    TextExtract
}

public enum FileTokenAudience
{
    Download,
    Preview,
    Stream,
    Upload
}