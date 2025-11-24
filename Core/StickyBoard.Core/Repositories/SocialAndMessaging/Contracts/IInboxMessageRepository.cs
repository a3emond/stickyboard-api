using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface IInboxMessageRepository : IRepository<InboxMessage>
{
    Task<IEnumerable<InboxMessage>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid messageId, CancellationToken ct);
}