using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface ICardCommentRepository : IRepository<CardComment>
{
    Task<IEnumerable<CardComment>> GetByCardIdAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<CardComment>> GetThreadAsync(Guid cardId, Guid rootCommentId, CancellationToken ct);
}