using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;

[Table("mentions")]
public sealed class Mention : IEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("entity_type")]
    public EntityType EntityType { get; set; }

    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Column("mentioned_user")]
    public Guid MentionedUser { get; set; }

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}