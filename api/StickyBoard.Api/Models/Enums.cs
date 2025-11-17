namespace StickyBoard.Api.Models
{
    
    public enum ErrorCode
    {
        SERVER_ERROR,
        AUTH_INVALID,
        AUTH_EXPIRED,
        NOT_FOUND,
        FORBIDDEN,
        VALIDATION_ERROR,
        CONFLICT,
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
        guest
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

}