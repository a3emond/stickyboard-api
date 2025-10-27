using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Activities;

[Table("activities")]
public class Activity : IEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("board_id")] public Guid BoardId { get; set; }
    [Column("card_id")] public Guid? CardId { get; set; }
    [Column("actor_id")] public Guid? ActorId { get; set; }
    [Column("act_type")] public ActivityType ActType { get; set; }
    [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}