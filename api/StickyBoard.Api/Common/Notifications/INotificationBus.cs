namespace StickyBoard.Api.Common.Notifications;

public interface INotificationBus
{
    // Mention (@user in card, comment, message)
    Task NotifyMentionAsync(Guid mentionedUserId, Guid entityId, string entityType);

    // Reply (comment reply, message reply)
    Task NotifyReplyAsync(Guid repliedUserId, Guid entityId, string entityType);

    // Assignment (card.assignee changed)
    Task NotifyAssignmentAsync(Guid assigneeId, Guid cardId);

    // Direct inbox message (DM)
    Task NotifyDirectMessageAsync(Guid receiverId, Guid inboxMessageId);

    // Invite accepted / invite received
    Task NotifyInviteAsync(Guid userId, Guid inviteId);

    // General push notification (fallback)
    Task PushAsync(Guid userId, string title, string body, object? data = null);

    // Presence/typing (optional)
    Task NotifyPresenceAsync(Guid userId, string channel, string status);

    // Low-level: publish raw event (for advanced future use)
    Task PublishAsync(string topic, object payload);
}