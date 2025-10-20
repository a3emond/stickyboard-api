using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Models.Users;

namespace StickyBoard.Api.Models.CardsTagsAssignees;

[Table("cards")]
public class Card
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("Board")] public Guid BoardId { get; set; }
    [ForeignKey("Section")] public Guid? SectionId { get; set; }
    [ForeignKey("Tab")] public Guid? TabId { get; set; }
    public CardType Type { get; set; }
    public string? Title { get; set; }
    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    [Column("ink_data")] public JsonDocument? InkData { get; set; }
    [Column("due_date")] public DateTime? DueDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Priority { get; set; }
    public CardStatus Status { get; set; } = CardStatus.open;
    [ForeignKey("Creator")] public Guid? CreatedBy { get; set; }
    [ForeignKey("Assignee")] public Guid? AssigneeId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 0;

    public User? Creator { get; set; }
    public User? Assignee { get; set; }
    public Board? Board { get; set; }
    public Section? Section { get; set; }
}