using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Boards;

namespace StickyBoard.Api.Models.SectionsAndTabs;

[Table("tabs")]
public class Tab
{
    [Key] public Guid Id { get; set; }
    public TabScope Scope { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    [ForeignKey("Section")] public Guid? SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    [Column("tab_type")] public string TabType { get; set; } = "custom";
    [Column("layout_config")] public JsonDocument LayoutConfig { get; set; } = JsonDocument.Parse("{}");
    public int Position { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    public Board? Board { get; set; }
    public Section? Section { get; set; }
}