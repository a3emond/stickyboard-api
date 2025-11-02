namespace StickyBoard.Api.Models
{
    
    public enum ErrorCode
    {
        SERVER_ERROR,
        AUTH_INVALID,
        AUTH_EXPIRED,
        NOT_FOUND,
        FORBIDDEN,
        VALIDATION_ERROR
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
    // Organizations
    // ============================
    public enum OrgRole
    {
        owner,
        admin,
        moderator,
        member,
        guest
    }

    // ============================
    // Boards & Permissions
    // ============================
    public enum BoardRole
    {
        owner,
        editor,
        commenter,
        viewer
    }

    public enum BoardVisibility
    {
        private_,
        shared,
        public_
    }

    // ============================
    // Tabs
    // ============================
    public enum TabScope
    {
        board,
        section
    }

    public enum TabType
    {
        board,
        calendar,
        timeline,
        kanban,
        whiteboard,
        chat,
        metrics,
        custom
    }

    // ============================
    // Cards
    // ============================
    public enum CardType
    {
        note,
        task,
        event_,
        drawing
    }

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
    public enum MessageType
    {
        invite,
        system,
        direct,
        org_invite
    }

    public enum MessageStatus
    {
        unread,
        read,
        archived
    }

    public enum RelationStatus
    {
        active_,
        blocked,
        inactive
    }
}