using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.LinksClustersActivities;

[Table("clusters")]
public class Cluster
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    [Column("cluster_type")] public ClusterType ClusterType { get; set; }
    [Column("rule_def")] public JsonDocument? RuleDef { get; set; }
    [Column("visual_meta")] public JsonDocument VisualMeta { get; set; } = JsonDocument.Parse("{}");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}