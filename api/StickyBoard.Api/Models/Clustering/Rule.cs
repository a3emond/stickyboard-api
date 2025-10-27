using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Clustering;

[Table("rules")]
public class Rule : IEntityUpdatable
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("board_id")] public Guid BoardId { get; set; }
    [Column("definition")] public JsonDocument Definition { get; set; } = JsonDocument.Parse("{}");
    [Column("enabled")] public bool Enabled { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}