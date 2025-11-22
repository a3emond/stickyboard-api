using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct);
}