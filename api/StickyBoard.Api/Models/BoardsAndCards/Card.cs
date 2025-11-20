using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("cards")]
public class Card : IVersionedEntity, ISoftDeletable
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("board_id")]
    public Guid BoardId { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("markdown")]
    public string Markdown { get; set; } = string.Empty;

    [Column("ink_data")]
    public JsonDocument? InkData { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("checklist")]
    public JsonDocument? Checklist { get; set; }

    [Column("priority")]
    public int? Priority { get; set; }

    [Column("status")]
    public CardStatus Status { get; set; } = CardStatus.open;

    [Column("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [Column("assignee")]
    public Guid? Assignee { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("last_edited_by")]
    public Guid? LastEditedBy { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}