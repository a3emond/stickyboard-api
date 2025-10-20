using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.CardsTagsAssignees;

namespace StickyBoard.Api.Models.SectionsAndTabs;

[Table("sections")]
public class Section
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    [Column("layout_meta")] public JsonDocument LayoutMeta { get; set; } = JsonDocument.Parse("{}");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    public Board? Board { get; set; }
    public ICollection<Card>? Cards { get; set; }
}