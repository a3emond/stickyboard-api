using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.RulesFilesOperations;

[Table("operations")]
public class Operation
{
    [Key] public Guid Id { get; set; }
    [Column("device_id")] public string DeviceId { get; set; } = string.Empty;
    [ForeignKey("User")] public Guid UserId { get; set; }
    public EntityType Entity { get; set; }
    [Column("entity_id")] public Guid EntityId { get; set; }
    [Column("op_type")] public string OpType { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    [Column("version_prev")] public int? VersionPrev { get; set; }
    [Column("version_next")] public int? VersionNext { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}