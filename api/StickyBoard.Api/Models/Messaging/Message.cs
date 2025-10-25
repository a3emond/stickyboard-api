using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Messaging;

[Table("messages")]
public class Message : IEntity
{
    [Key,Column("id")] public Guid Id { get; set; }
    [Column("sender_id")] public Guid? SenderId { get; set; }
    [Column("receiver_id")] public Guid ReceiverId { get; set; }
    [Column("subject")] public string? Subject { get; set; }
    [Column("body")] public string? Body { get; set; }
    [Column("type")] public string Type { get; set; } = "general";
    [Column("related_board")] public Guid? RelatedBoardId { get; set; }
    [Column("related_org")] public Guid? RelatedOrganizationId { get; set; }
    [Column("status")] public string Status { get; set; } = "unread";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("invites")]
public class Invite : IEntity
{
    [Key,Column("id")] public Guid Id { get; set; }
    [Column("sender_id")] public Guid SenderId { get; set; }
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("board_id")] public Guid? BoardId { get; set; }
    [Column("org_id")] public Guid? OrganizationId { get; set; }
    [Column("role")] public string? Role { get; set; }
    [Column("token")] public string Token { get; set; } = string.Empty;
    [Column("accepted")] public bool Accepted { get; set; } = false;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
}

public class Notification
{
    // info: not yet a database entity - to be implemented later if needed.
}


