using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.RulesFilesOperations;

[Table("files")]
public class FileRecord
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Owner")] public Guid OwnerId { get; set; }
    [ForeignKey("Board")] public Guid? BoardId { get; set; }
    [ForeignKey("Card")] public Guid? CardId { get; set; }
    [Column("storage_key")] public string StorageKey { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    [Column("mime_type")] public string? MimeType { get; set; }
    [Column("size_bytes")] public long? SizeBytes { get; set; }
    public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}