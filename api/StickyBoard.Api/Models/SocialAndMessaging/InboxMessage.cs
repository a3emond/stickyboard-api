using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;

[Table("inbox_messages")]
public sealed class InboxMessage : IEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sender_id")]
    public Guid? SenderId { get; set; }

    [Column("receiver_id")]
    public Guid ReceiverId { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}