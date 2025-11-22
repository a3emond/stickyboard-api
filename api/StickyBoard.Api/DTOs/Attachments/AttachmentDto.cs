using System.Text.Json;

namespace StickyBoard.Api.DTOs.Attachments;

// READ
public sealed class AttachmentDto
{
    public Guid Id { get; set; }

    public Guid? WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? CardId { get; set; }

    public string Filename { get; set; } = string.Empty;
    public string? Mime { get; set; }
    public long? ByteSize { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public bool IsPublic { get; set; }

    public string Status { get; set; } = "ready";
    public JsonDocument? Meta { get; set; }

    public Guid? UploadedBy { get; set; }
    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// CREATE
public sealed class AttachmentCreateDto
{
    public Guid? WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? CardId { get; set; }

    public string Filename { get; set; } = string.Empty;
    public string? Mime { get; set; }
    public long? ByteSize { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public bool IsPublic { get; set; }

    public string Status { get; set; } = "ready";
    public JsonDocument? Meta { get; set; }
}

// UPDATE (metadata only)
public sealed class AttachmentUpdateDto
{
    public string? Filename { get; set; }
    public string? Mime { get; set; }
    public long? ByteSize { get; set; }

    public bool? IsPublic { get; set; }
    public string? Status { get; set; }
    public JsonDocument? Meta { get; set; }

    public int Version { get; set; }
}