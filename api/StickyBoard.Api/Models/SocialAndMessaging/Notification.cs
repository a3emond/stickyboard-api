using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;

[Table("notifications")]
public sealed class Notification : IEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("type")]
    public NotificationType Type { get; set; }

    [Column("entity_id")]
    public Guid? EntityId { get; set; }

    [Column("read")]
    public bool Read { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}