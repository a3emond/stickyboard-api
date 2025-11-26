using StickyBoard.Core.Models;

namespace StickyBoard.Core.DTOs.Attachments;

public sealed class AttachmentInitRequest
{
    public Guid? WorkspaceId { get; set; }
    public Guid BoardId { get; set; }
    public Guid? CardId { get; set; }

    public string Filename { get; set; } = null!;
    public string Mime { get; set; } = null!;
    public long ByteSize { get; set; }
    public bool IsPublic { get; set; } = false;
}

public sealed class AttachmentInitResult
{
    public Guid AttachmentId { get; set; }
    public string StoragePath { get; set; } = null!;
    public string UploadUrl { get; set; } = null!;
}

public sealed class AttachmentDownloadResult
{
    public Guid AttachmentId { get; set; }
    public string Variant { get; set; } = "original";
    public string DownloadUrl { get; set; } = null!;
}

public sealed class AttachmentMetaDto
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = null!;
    public string Mime { get; set; } = null!;
    public long? ByteSize { get; set; }
    public AttachmentStatus Status { get; set; }
    public IReadOnlyList<VariantMetaDto> Variants { get; set; } = [];
}

public sealed class VariantMetaDto
{
    public string Variant { get; set; } = null!;
    public long? ByteSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public AttachmentStatus Status { get; set; }
    public int Version { get; set; }
}

public sealed class VariantRegisterRequest
{
    public Guid AttachmentId { get; set; }
    public AttachmentVariantType Variant { get; set; }

    public string StoragePath { get; set; } = null!;
    public string Mime { get; set; } = null!;
    public long? ByteSize { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }

    public byte[]? ChecksumSha256 { get; set; }
}