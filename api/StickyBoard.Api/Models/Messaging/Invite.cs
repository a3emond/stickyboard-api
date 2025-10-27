using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Messaging;

[Table("invites")]
public class Invite : IEntity
{
    [Key,Column("id")] public Guid Id { get; set; }
    [Column("sender_id")] public Guid SenderId { get; set; }
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("board_id")] public Guid? BoardId { get; set; }
    [Column("org_id")] public Guid? OrganizationId { get; set; }
    [Column("role")] public BoardRole? Role { get; set; }
    [Column("token")] public string Token { get; set; } = string.Empty;
    [Column("accepted")] public bool Accepted { get; set; } = false;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
}