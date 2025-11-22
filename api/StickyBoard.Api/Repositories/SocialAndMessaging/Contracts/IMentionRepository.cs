using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface IMentionRepository : IRepository<Mention>
{
    Task<IEnumerable<Mention>> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<IEnumerable<Mention>> GetForEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct);
}