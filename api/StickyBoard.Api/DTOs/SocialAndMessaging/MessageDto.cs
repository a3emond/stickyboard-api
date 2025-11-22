using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class MessageDto
{
    public Guid Id { get; set; }
    public MessageChannel Channel { get; set; }

    public Guid? BoardId { get; set; }
    public Guid? ViewId { get; set; }
    public Guid? SenderId { get; set; }
    public Guid? ParentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class MessageCreateDto
{
    public MessageChannel Channel { get; set; }

    public Guid? BoardId { get; set; }
    public Guid? ViewId { get; set; }
    public Guid? ParentId { get; set; }

    public string Content { get; set; } = string.Empty;
}

public sealed class MessageUpdateDto
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}