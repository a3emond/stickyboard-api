using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.RulesFilesOperations;

[Table("rules")]
public class Rule
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    public JsonDocument Definition { get; set; } = JsonDocument.Parse("{}");
    public bool Enabled { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}