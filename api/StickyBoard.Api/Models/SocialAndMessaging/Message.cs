using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;

[Table("messages")]
public sealed class Message : IEntityUpdatable, ISoftDeletable
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("channel")]
    public MessageChannel Channel { get; set; }

    [Column("board_id")]
    public Guid? BoardId { get; set; }

    [Column("view_id")]
    public Guid? ViewId { get; set; }

    [Column("sender_id")]
    public Guid? SenderId { get; set; }

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}