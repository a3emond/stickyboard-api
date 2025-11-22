using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface IMentionService
{
    Task<MentionDto> CreateAsync(Guid authorId, MentionCreateDto dto, CancellationToken ct);

    Task<IEnumerable<MentionDto>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<IEnumerable<MentionDto>> GetForEntityAsync(EntityType entityType, Guid entityId, CancellationToken ct);
}