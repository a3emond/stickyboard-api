using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface IInboxMessageRepository : IRepository<InboxMessage>
{
    Task<IEnumerable<InboxMessage>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid messageId, CancellationToken ct);
}