using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging
{
    [Table("messages")]
    public class Message : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("sender_id")]
        public Guid? SenderId { get; set; }

        [Column("receiver_id")]
        public Guid ReceiverId { get; set; }

        [Column("subject")]
        public string? Subject { get; set; }

        [Column("body")]
        public string Body { get; set; } = string.Empty;

        [Column("type")]
        public MessageType Type { get; set; } = MessageType.direct;

        [Column("related_board")]
        public Guid? RelatedBoard { get; set; }

        [Column("related_org")]
        public Guid? RelatedOrg { get; set; }

        [Column("status")]
        public MessageStatus Status { get; set; } = MessageStatus.unread;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}