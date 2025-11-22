using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class CardCommentDto
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }

    public Guid? ParentId { get; set; }
    public Guid? UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class CardCommentCreateDto
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

public sealed class CardCommentUpdateDto
{
    public string Content { get; set; } = string.Empty;
}