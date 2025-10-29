namespace StickyBoard.Api.DTOs.Sync
{
    public sealed class SyncCommitResultDto
    {
        public int AcceptedCount { get; init; }
        public List<Guid> OperationIds { get; init; } = [];
        public DateTime ServerTime { get; init; }
    }
}