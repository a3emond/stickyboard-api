using StickyBoard.Api.DTOs.Cards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Services
{
    public sealed class CardRelationsService
    {
        private readonly TagRepository _tags;
        private readonly CardTagRepository _cardTags;
        private readonly LinkRepository _links;
        private readonly CardRepository _cards;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public CardRelationsService(
            TagRepository tags,
            CardTagRepository cardTags,
            LinkRepository links,
            CardRepository cards,
            BoardRepository boards,
            PermissionRepository permissions)
        {
            _tags = tags;
            _cardTags = cardTags;
            _links = links;
            _cards = cards;
            _boards = boards;
            _permissions = permissions;
        }

        private async Task EnsureCanEditAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor))
                throw new UnauthorizedAccessException("User not allowed to modify this board.");
        }

        // ----------------------------------------------------------------------
        // TAGS
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<TagDto>> GetTagsAsync(CancellationToken ct)
        {
            var tags = await _tags.GetAllAsync(ct);
            return tags.Select(Map);
        }

        public async Task<IEnumerable<TagDto>> SearchTagsAsync(string query, CancellationToken ct)
        {
            var tags = await _tags.SearchAsync(query, ct);
            return tags.Select(Map);
        }

        public async Task<Guid> CreateTagAsync(CreateTagDto dto, CancellationToken ct)
        {
            var tag = new Tag { Name = dto.Name };
            return await _tags.CreateAsync(tag, ct);
        }

        public async Task<IEnumerable<TagDto>> GetTagsForCardAsync(Guid cardId, CancellationToken ct)
        {
            var cardTags = await _cardTags.GetByCardAsync(cardId, ct);
            var tagIds = cardTags.Select(ctg => ctg.TagId);
            if (!tagIds.Any()) return [];
            var tags = new List<TagDto>();
            foreach (var id in tagIds)
            {
                var t = await _tags.GetByIdAsync(id, ct);
                if (t != null) tags.Add(Map(t));
            }
            return tags;
        }

        public async Task AssignTagsAsync(Guid userId, Guid cardId, IEnumerable<Guid> tagIds, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct);
            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureCanEditAsync(userId, card.BoardId, ct);

            // Remove existing
            await _cardTags.DeleteAsync(cardId, ct);

            // Insert new set
            foreach (var tagId in tagIds)
                await _cardTags.CreateAsync(new CardTag { CardId = cardId, TagId = tagId }, ct);
        }

        public async Task<bool> RemoveTagAsync(Guid userId, Guid cardId, Guid tagId, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct);
            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureCanEditAsync(userId, card.BoardId, ct);
            return await _cardTags.RemoveTagAsync(cardId, tagId, ct);
        }

        // ----------------------------------------------------------------------
        // LINKS
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<LinkDto>> GetLinksFromAsync(Guid cardId, CancellationToken ct)
        {
            var links = await _links.GetLinksFromAsync(cardId, ct);
            return links.Select(Map);
        }

        public async Task<IEnumerable<LinkDto>> GetLinksToAsync(Guid cardId, CancellationToken ct)
        {
            var links = await _links.GetLinksToAsync(cardId, ct);
            return links.Select(Map);
        }

        public async Task<Guid> CreateLinkAsync(Guid userId, Guid fromCard, CreateLinkDto dto, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(fromCard, ct);
            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureCanEditAsync(userId, card.BoardId, ct);

            var link = new Link
            {
                FromCard = fromCard,
                ToCard = dto.ToCard,
                RelType = dto.RelType,
                CreatedBy = userId
            };

            return await _links.CreateAsync(link, ct);
        }

        public async Task<bool> UpdateLinkAsync(Guid userId, Guid linkId, UpdateLinkDto dto, CancellationToken ct)
        {
            var link = await _links.GetByIdAsync(linkId, ct);
            if (link is null)
                return false;

            var fromCard = await _cards.GetByIdAsync(link.FromCard, ct);
            if (fromCard is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureCanEditAsync(userId, fromCard.BoardId, ct);

            link.RelType = dto.RelType;
            return await _links.UpdateAsync(link, ct);
        }

        public async Task<bool> DeleteLinkAsync(Guid userId, Guid linkId, CancellationToken ct)
        {
            var link = await _links.GetByIdAsync(linkId, ct);
            if (link is null)
                return false;

            var fromCard = await _cards.GetByIdAsync(link.FromCard, ct);
            if (fromCard is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureCanEditAsync(userId, fromCard.BoardId, ct);
            return await _links.DeleteAsync(linkId, ct);
        }
        
        
        private static TagDto Map(Tag t) => new() { Id = t.Id, Name = t.Name };
        private static LinkDto Map(Link l) => new()
        {
            Id = l.Id,
            FromCard = l.FromCard,
            ToCard = l.ToCard,
            RelType = l.RelType,
            CreatedBy = l.CreatedBy,
            CreatedAt = l.CreatedAt
        };
    }
}
