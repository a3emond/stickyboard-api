using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

public sealed class ContactEntryDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public ContactStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}