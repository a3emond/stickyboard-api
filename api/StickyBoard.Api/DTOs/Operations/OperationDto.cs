using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Operations
{
    public sealed class CreateOperationDto
    {
        public string? DeviceId { get; set; }
        public EntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
        public string OpType { get; set; } = string.Empty;
        public Dictionary<string, object>? PayloadJson { get; set; }
        public int? VersionPrev { get; set; }
        public int? VersionNext { get; set; }
    }

    public sealed class OperationQueryDto
    {
        public string? DeviceId { get; set; }
        public DateTime? Since { get; set; }
        public Guid? UserId { get; set; }
        public int? Limit { get; set; } = 100;
    }

    public sealed class OperationDto
    {
        public Guid Id { get; set; }
        public string? DeviceId { get; set; }
        public Guid UserId { get; set; }
        public EntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
        public string OpType { get; set; } = string.Empty;
        public Dictionary<string, object>? PayloadJson { get; set; }
        public long? VersionPrev { get; set; }
        public long? VersionNext { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
    }

    public sealed class OperationMaintenanceResultDto
    {
        public int DeletedCount { get; set; }
        public int ProcessedCount { get; set; }
    }
}