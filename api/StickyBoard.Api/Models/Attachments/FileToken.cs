using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Attachments;

[Table("file_tokens")]
public sealed class FileToken : IEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("attachment_id")]
    public Guid AttachmentId { get; set; }

    [Column("variant")]
    public string? Variant { get; set; }

    [Column("secret")]
    public byte[] Secret { get; set; } = null!;

    [Column("audience")]
    public string Audience { get; set; } = "download";

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("revoked")]
    public bool Revoked { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}