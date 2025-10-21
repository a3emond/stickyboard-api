// ===========================================================
// StickyBoard API Models (C# / Entity Framework Core)
// Matching PostgreSQL Schema – Final 2025-10-19
// ===========================================================
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StickyBoard.Api.Models
{
    // =======================================================
    // ENUMS
    // =======================================================
    public enum UserRole { user, admin, moderator }
    public enum BoardVisibility { private_, shared, public_ }
    public enum BoardRole { owner, editor, commenter, viewer }
    public enum TabScope { board, section }
    public enum CardType { note, task, event_, drawing }
    public enum CardStatus { open, in_progress, blocked, done, archived }
    public enum LinkType { references_, duplicate_of, relates_to, blocks, depends_on }
    public enum ClusterType { manual, rule, similarity }
    public enum ActivityType { card_created, card_updated, card_moved, comment_added, status_changed, assignee_changed, link_added, link_removed, rule_triggered }
    public enum EntityType { user, board, section, tab, card, link, cluster, rule, file }

    // Worker Queue Related
    public enum JobKind { RuleExecutor, ClusterRebuilder, SearchIndexer, SyncCompactor, NotificationDispatcher, AnalyticsExporter, Generic }
    public enum JobStatus { queued, running, succeeded, failed, canceled, dead }

}