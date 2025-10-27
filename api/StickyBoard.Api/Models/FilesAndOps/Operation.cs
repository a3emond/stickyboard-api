using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.FilesAndOps;

[Table("operations")]
public class Operation : IEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("device_id")] public string DeviceId { get; set; } = string.Empty;
    [Column("user_id")] public Guid UserId { get; set; }
    [Column("entity")] public EntityType Entity { get; set; }
    [Column("entity_id")] public Guid EntityId { get; set; }
    [Column("op_type")] public string OpType { get; set; } = string.Empty;
    [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    [Column("version_prev")] public int? VersionPrev { get; set; }
    [Column("version_next")] public int? VersionNext { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}