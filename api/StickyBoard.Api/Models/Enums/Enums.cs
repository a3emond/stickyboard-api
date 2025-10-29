namespace StickyBoard.Api.Models.Enums
{
    // =======================================================
    // User & Authorization
    // =======================================================
    public enum UserRole { user, admin, moderator }
    public enum BoardRole { owner, editor, commenter, viewer }
    public enum OrgRole { owner, admin, moderator, member, guest }

    // =======================================================
    // Visibility & Structure
    // =======================================================
    public enum BoardVisibility { private_, shared, public_ }
    public enum TabScope { board, section }

    // =======================================================
    // Cards, Links & Clusters
    // =======================================================
    public enum CardType { note, task, @event, drawing }
    public enum CardStatus { open, in_progress, blocked, done, archived }
    public enum LinkType { references, duplicate_of, relates_to, blocks, depends_on }
    public enum ClusterType { manual, rule, similarity }

    // =======================================================
    // Activity & Entity Types
    // =======================================================
    public enum ActivityType
    {
        card_created, card_updated, card_moved, comment_added,
        status_changed, assignee_changed, link_added, link_removed,
        rule_triggered, board_changed, cluster_changed, rule_changed
    }

    public enum EntityType
    {
        user, board, section, tab, card, link, cluster, rule, file
    }

    // =======================================================
    // Worker / Jobs
    // =======================================================
    public enum JobKind
    {
        RuleExecutor, ClusterRebuilder, SearchIndexer,
        SyncCompactor, NotificationDispatcher, AnalyticsExporter, Generic
    }

    public enum JobStatus { queued, running, succeeded, failed, canceled, dead }

    // =======================================================
    // Messaging & Social
    // =======================================================
    public enum MessageType { invite, system, direct, org_invite }

    public enum MessageStatus
    {
        unread,
        read,
        archived
    }

    public enum RelationStatus { active, blocked, inactive }
}
