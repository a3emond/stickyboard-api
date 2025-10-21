using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.SectionsAndTabs
{
    [Table("sections")]
    public class Section : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("title")] public string Title { get; set; } = string.Empty;
        [Column("position")] public int Position { get; set; }
        [Column("layout_meta")] public JsonDocument LayoutMeta { get; set; } = JsonDocument.Parse("{}");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Table("tabs")]
    public class Tab : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("scope")] public TabScope Scope { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("section_id")] public Guid? SectionId { get; set; }
        [Column("title")] public string Title { get; set; } = string.Empty;
        [Column("tab_type")] public string TabType { get; set; } = "custom";
        [Column("layout_config")] public JsonDocument LayoutConfig { get; set; } = JsonDocument.Parse("{}");
        [Column("position")] public int Position { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}