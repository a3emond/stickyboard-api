using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Core.Models.Base;

namespace StickyBoard.Core.Models.Attachments;

[Table("attachment_variants")]
public sealed class AttachmentVariant : IEntityUpdatable
{
    [Column("id")] public Guid Id { get; set; }

    [Column("parent_id")] public Guid ParentId { get; set; }

    [Column("variant")] public AttachmentVariantType Variant { get; init; }

    [Column("mime")] public string Mime { get; set; } = null!;

    [Column("byte_size")] public long? ByteSize { get; set; }

    [Column("width")] public int? Width { get; set; }

    [Column("height")] public int? Height { get; set; }

    [Column("duration_ms")] public int? DurationMs { get; set; }

    [Column("storage_path")] public string StoragePath { get; set; } = null!;

    [Column("status")] public AttachmentStatus Status { get; set; } = AttachmentStatus.Pending;

    [Column("checksum_sha256")] public byte[]? ChecksumSha256 { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}