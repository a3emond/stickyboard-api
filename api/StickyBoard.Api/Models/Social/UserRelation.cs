using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Social;

[Table("user_relations")]
public class UserRelation : IEntity
{
    [Key, Column("user_id", Order = 0)] public Guid UserId { get; set; }
    [Key, Column("friend_id", Order = 1)] public Guid FriendId { get; set; }
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("requested_at")] public DateTime RequestedAt { get; set; }
    [Column("accepted_at")] public DateTime? AcceptedAt { get; set; }
}