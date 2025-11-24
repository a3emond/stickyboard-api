using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface ICardCommentRepository : IRepository<CardComment>
{
    Task<IEnumerable<CardComment>> GetByCardIdAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<CardComment>> GetThreadAsync(Guid cardId, Guid rootCommentId, CancellationToken ct);
}