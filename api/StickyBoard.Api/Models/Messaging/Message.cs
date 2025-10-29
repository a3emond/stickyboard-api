using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Messaging;

[Table("messages")]
public class Message : IEntity
{
    [Key,Column("id")] public Guid Id { get; set; }
    [Column("sender_id")] public Guid? SenderId { get; set; }
    [Column("receiver_id")] public Guid ReceiverId { get; set; }
    [Column("subject")] public string? Subject { get; set; }
    [Column("body")] public string? Body { get; set; }
    [Column("type")] public MessageType Type { get; set; } = MessageType.direct;
    [Column("related_board")] public Guid? RelatedBoardId { get; set; }
    [Column("related_org")] public Guid? RelatedOrganizationId { get; set; }
    [Column("status")] public MessageStatus Status { get; set; } = MessageStatus.unread;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

public class Notification
{
    // info: not yet a database entity - to be implemented later if needed.
}


