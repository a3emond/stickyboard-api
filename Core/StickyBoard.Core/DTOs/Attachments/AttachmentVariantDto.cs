using StickyBoard.Core.Models;

namespace StickyBoard.Core.DTOs.Attachments;

public sealed class AttachmentVariantDto
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }

    public AttachmentVariantType Variant { get; set; }
    public string Mime { get; set; } = string.Empty;

    public long? ByteSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public AttachmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AttachmentVariantCreateDto
{
    public Guid ParentId { get; set; }

    public AttachmentVariantType Variant { get; set; }
    public string Mime { get; set; } = string.Empty;

    public long? ByteSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public AttachmentStatus Status { get; set; }

    public byte[]? ChecksumSha256 { get; set; }
}

public sealed class AttachmentVariantUpdateDto
{
    public string? Mime { get; set; }

    public long? ByteSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }

    public string? StoragePath { get; set; }
    public AttachmentStatus? Status { get; set; }

    public byte[]? ChecksumSha256 { get; set; }
}