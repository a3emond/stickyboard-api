using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Models.Users;

namespace StickyBoard.Api.Models.Boards;

[Table("boards")]
public class Board
{
    [Key]
    public Guid Id { get; set; }
    [ForeignKey("Owner")] public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;
    public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument Rules { get; set; } = JsonDocument.Parse("[]");
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    public User? Owner { get; set; }
    public ICollection<Section>? Sections { get; set; }
}