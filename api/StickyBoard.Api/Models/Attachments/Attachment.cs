using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Attachments;

[Table("attachments")]
public sealed class Attachment : IEntityUpdatable, ISoftDeletable, IVersionedEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("workspace_id")]
    public Guid? WorkspaceId { get; set; }

    [Column("board_id")]
    public Guid? BoardId { get; set; }

    [Column("card_id")]
    public Guid? CardId { get; set; }

    [Column("filename")]
    public string Filename { get; set; } = null!;

    [Column("mime")]
    public string? Mime { get; set; }

    [Column("byte_size")]
    public long? ByteSize { get; set; }

    [Column("checksum_sha256")]
    public byte[]? ChecksumSha256 { get; set; }

    [Column("storage_path")]
    public string StoragePath { get; set; } = null!;

    [Column("is_public")]
    public bool IsPublic { get; set; }

    [Column("status")]
    public string Status { get; set; } = "ready";

    [Column("meta")]
    public JsonDocument? Meta { get; set; }

    [Column("uploaded_by")]
    public Guid? UploadedBy { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}