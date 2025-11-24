using StickyBoard.Core.Models;
using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface IMentionRepository : IRepository<Mention>
{
    Task<IEnumerable<Mention>> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<IEnumerable<Mention>> GetForEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct);
}