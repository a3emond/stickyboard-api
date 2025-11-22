namespace StickyBoard.Api.DTOs.SocialAndMessaging;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class InboxMessageDto
{
    public Guid Id { get; set; }

    public Guid? SenderId { get; set; }
    public Guid ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class InboxMessageCreateDto
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}