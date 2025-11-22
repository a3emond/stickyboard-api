using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class MentionDto
{
    public Guid Id { get; set; }

    public EntityType EntityType { get; set; } 
    public Guid EntityId { get; set; }

    public Guid MentionedUser { get; set; }
    public Guid? AuthorId { get; set; }

    public DateTime CreatedAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class MentionCreateDto
{
    public EntityType EntityType { get; set; } 
    public Guid EntityId { get; set; }
    public Guid MentionedUser { get; set; }
}