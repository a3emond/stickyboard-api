// StickyBoard.Core/Infrastructure/Db/DataSourceFactory.cs

using Npgsql;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Infrastructure.Db;

public static class DataSourceFactory
{
    public static NpgsqlDataSource Create(string connectionString)
    {
        var b = new NpgsqlDataSourceBuilder(connectionString);

        b.MapEnum<UserRole>("user_role");
        b.MapEnum<WorkspaceRole>("workspace_role");
        b.MapEnum<ViewType>("view_type");
        b.MapEnum<CardStatus>("card_status");
        b.MapEnum<MessageChannel>("message_channel");
        b.MapEnum<NotificationType>("notification_type");
        b.MapEnum<InviteStatus>("invite_status");
        b.MapEnum<InviteScope>("invite_scope");
        b.MapEnum<ContactStatus>("contact_status");
        b.MapEnum<EntityType>("entity_type");
        b.MapEnum<WorkerJobKind>("worker_job_kind");
        b.MapEnum<WorkerJobStatus>("worker_job_status");
        b.MapEnum<PushProvider>("push_provider");
        b.MapEnum<NotificationChannel>("notification_channel");
        b.MapEnum<SyncScopeType>("sync_scope_type");
        b.MapEnum<AttachmentStatus>("attachment_status");
        b.MapEnum<AttachmentVariantType>("attachment_variant_type");
        b.MapEnum<OutboxTopic>("outbox_topic");
        b.MapEnum<OutboxOperation>("outbox_operation");
        b.MapEnum<FileTokenAudience>("file_token_audience");

        return b.Build();
    }
}