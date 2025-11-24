using StickyBoard.Core.DTOs.SocialAndMessaging;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Services.SocialAndMessaging.Contracts;

public interface IMentionService
{
    Task<MentionDto> CreateAsync(Guid authorId, MentionCreateDto dto, CancellationToken ct);

    Task<IEnumerable<MentionDto>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<IEnumerable<MentionDto>> GetForEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct);
}