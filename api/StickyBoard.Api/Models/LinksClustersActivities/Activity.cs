using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.LinksClustersActivities;

[Table("activities")]
public class Activity
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    [ForeignKey("Card")] public Guid? CardId { get; set; }
    [ForeignKey("Actor")] public Guid? ActorId { get; set; }
    [Column("act_type")] public ActivityType ActType { get; set; }
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}