using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Message>> GetByViewAsync(Guid viewId, CancellationToken ct);
}