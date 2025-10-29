using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Operations
{
    // ------------------------------------------------------------
    // CREATE / APPEND REQUEST
    // ------------------------------------------------------------
    public sealed class CreateOperationDto
    {
        public string? DeviceId { get; set; }
        public EntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
        public string OpType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public int? VersionPrev { get; set; }
        public int? VersionNext { get; set; }
    }

    // ------------------------------------------------------------
    // QUERY FILTERS
    // ------------------------------------------------------------
    public sealed class OperationQueryDto
    {
        public String? DeviceId { get; set; }
        public DateTime? Since { get; set; }
        public Guid? UserId { get; set; }
        public int? Limit { get; set; } = 100;
    }

    // ------------------------------------------------------------
    // RESPONSE DTO
    // ------------------------------------------------------------
    public sealed class OperationDto
    {
        public Guid Id { get; set; }
        public String? DeviceId { get; set; }
        public Guid UserId { get; set; }
        public EntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
        public string OpType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public long? VersionPrev { get; set; }
        public long? VersionNext { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
    }

    // ------------------------------------------------------------
    // MAINTENANCE RESPONSE
    // ------------------------------------------------------------
    public sealed class OperationMaintenanceResultDto
    {
        public int DeletedCount { get; set; }
        public int ProcessedCount { get; set; }
    }
}
