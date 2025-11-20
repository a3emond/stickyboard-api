using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("views")]
public class View : IVersionedEntity, ISoftDeletable
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("board_id")]
    public Guid BoardId { get; set; }
    [Column("title")]
    public string Title { get; set; } = null!;
    [Column("type")]
    public ViewType Type { get; set; }
    [Column("layout")]
    public JsonDocument Layout { get; set; } = JsonDocument.Parse("{}");
    [Column("position")]
    public int Position { get; set; }
    [Column("version")]
    public int Version { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

