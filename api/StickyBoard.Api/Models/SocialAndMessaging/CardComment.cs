using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;

[Table("card_comments")]
public class CardComment : IEntityUpdatable, ISoftDeletable
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("card_id")]
    public Guid CardId { get; set; }
    [Column("parent_id")]
    public Guid? ParentId { get; set; }
    [Column("user_id")]
    public Guid? UserId { get; set; }
    [Column("content")]
    public string Content { get; set; } = null!;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    
}
