using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Cards
{
    [Table("cards")]
    public class Card : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("section_id")] public Guid? SectionId { get; set; }
        [Column("tab_id")] public Guid? TabId { get; set; }
        [Column("type")] public CardType Type { get; set; }
        [Column("title")] public string? Title { get; set; }
        [Column("content")] public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
        [Column("ink_data")] public JsonDocument? InkData { get; set; }
        [Column("due_date")] public DateTime? DueDate { get; set; }
        [Column("start_time")] public DateTime? StartTime { get; set; }
        [Column("end_time")] public DateTime? EndTime { get; set; }
        [Column("priority")] public int? Priority { get; set; }
        [Column("status")] public CardStatus Status { get; set; } = CardStatus.open;
        [Column("created_by")] public Guid? CreatedBy { get; set; }
        [Column("assignee_id")] public Guid? AssigneeId { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
        [Column("version")] public int Version { get; set; } = 0;
    }
}
