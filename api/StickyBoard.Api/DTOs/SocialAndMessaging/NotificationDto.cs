using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class NotificationDto
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public NotificationType Type { get; set; }

    public Guid? EntityId { get; set; }

    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

// ------------------------------------------------------------
// WRITE (internal use)
// ------------------------------------------------------------
public sealed class NotificationCreateDto
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? EntityId { get; set; }
}