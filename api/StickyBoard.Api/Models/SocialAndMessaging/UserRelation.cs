using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging
{
    [Table("user_relations")]
    public class UserRelation : IEntityUpdatable
    {
        [Key, Column("user_id", Order = 0)]
        public Guid UserId { get; set; }

        [Key, Column("friend_id", Order = 1)]
        public Guid FriendId { get; set; }

        [Column("status")]
        public RelationStatus Status { get; set; } = RelationStatus.active_;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

}