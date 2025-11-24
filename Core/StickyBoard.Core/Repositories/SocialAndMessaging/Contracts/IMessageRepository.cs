using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Message>> GetByViewAsync(Guid viewId, CancellationToken ct);
}