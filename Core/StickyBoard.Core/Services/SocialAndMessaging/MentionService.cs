using StickyBoard.Core.DTOs.SocialAndMessaging;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Core.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Core.Services.SocialAndMessaging;

public sealed class MentionService : IMentionService
{
    private readonly IMentionRepository _mentions;

    public MentionService(IMentionRepository mentions)
    {
        _mentions = mentions;
    }

    public async Task<MentionDto> CreateAsync(Guid authorId, MentionCreateDto dto, CancellationToken ct)
    {
        var e = new Mention
        {
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            MentionedUser = dto.MentionedUser,
            AuthorId = authorId
        };

        var id = await _mentions.CreateAsync(e, ct);
        var created = await _mentions.GetByIdAsync(id, ct);

        return Map(created!);
    }

    public async Task<IEnumerable<MentionDto>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _mentions.GetForUserAsync(userId, ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<MentionDto>> GetForEntityAsync(EntityType entityType, Guid entityId,
        CancellationToken ct)
    {
        var list = await _mentions.GetForEntityAsync(entityType, entityId, ct);
        return list.Select(Map);
    }

    private static MentionDto Map(Mention m)
    {
        return new MentionDto
        {
            Id = m.Id,
            EntityType = m.EntityType,
            EntityId = m.EntityId,
            MentionedUser = m.MentionedUser,
            AuthorId = m.AuthorId,
            CreatedAt = m.CreatedAt
        };
    }
}