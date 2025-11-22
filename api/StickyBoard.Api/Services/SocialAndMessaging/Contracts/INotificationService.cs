using StickyBoard.Api.DTOs.SocialAndMessaging;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(NotificationCreateDto dto, CancellationToken ct);

    Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct);
}