namespace StickyBoard.Api.DTOs.Sync
{
    public sealed class SyncCommitRequestDto
    {
        public required string DeviceId { get; init; }
        public required List<SyncOperationItemDto> Operations { get; init; } = [];
    }
}