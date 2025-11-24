using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct);
}