namespace StickyBoard.Api.DTOs.Attachments;

public sealed class FileTokenDto
{
    public Guid Id { get; set; }

    public Guid AttachmentId { get; set; }
    public string? Variant { get; set; }

    public string Audience { get; set; } = "download";

    public DateTime ExpiresAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public bool Revoked { get; set; }
    public DateTime CreatedAt { get; set; }
}

// For creation (input)
public sealed class FileTokenCreateDto
{
    public Guid AttachmentId { get; set; }
    public string? Variant { get; set; }
    public string Audience { get; set; } = "download";
    public DateTime? ExpiresAt { get; set; } // null = default window
}