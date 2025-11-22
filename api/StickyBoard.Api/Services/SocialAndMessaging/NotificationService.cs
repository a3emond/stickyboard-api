using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<NotificationDto> CreateAsync(NotificationCreateDto dto, CancellationToken ct)
    {
        var e = new Notification
        {
            UserId = dto.UserId,
            Type = dto.Type,
            EntityId = dto.EntityId
        };

        var id = await _notifications.CreateAsync(e, ct);
        var created = await _notifications.GetByIdAsync(id, ct);

        return Map(created!);
    }

    public async Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct)
    {
        var list = await _notifications.GetForUserAsync(userId, unreadOnly, ct);
        return list.Select(Map);
    }

    public Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
        => _notifications.MarkAsReadAsync(notificationId, userId, ct);

    public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct)
        => _notifications.MarkAllAsReadAsync(userId, ct);

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Type = n.Type,
        EntityId = n.EntityId,
        Read = n.Read,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}