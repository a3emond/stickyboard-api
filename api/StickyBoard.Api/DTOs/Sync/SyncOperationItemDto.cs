using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Sync
{
    public sealed class SyncOperationItemDto
    {
        public required EntityType Entity { get; init; }        // board, card, section, etc.
        public required Guid EntityId { get; init; }
        public required string OpType { get; init; }            // create, update, delete
        public string? PayloadJson { get; init; }
        public int? VersionPrev { get; init; }
        public int? VersionNext { get; init; }
    }
}