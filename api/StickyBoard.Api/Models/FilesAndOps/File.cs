using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.FilesAndOps
{
    [Table("files")]
    public class File : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("owner_id")] public Guid OwnerId { get; set; }
        [Column("board_id")] public Guid? BoardId { get; set; }
        [Column("card_id")] public Guid? CardId { get; set; }
        [Column("storage_key")] public string StorageKey { get; set; } = string.Empty;
        [Column("filename")] public string FileName { get; set; } = string.Empty;
        [Column("mime_type")] public string? MimeType { get; set; }
        [Column("size_bytes")] public long SizeBytes { get; set; }
        [Column("meta")] public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}