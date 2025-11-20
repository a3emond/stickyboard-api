using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("boards")]
public sealed class Board : IEntity, ISoftDeletable
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("workspace_id")]
    public Guid WorkspaceId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("theme")]
    public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");

    [Column("meta")]
    public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}