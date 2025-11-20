using System.Text.Json;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.BoardsAndCards;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class CardDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }

    public string? Title { get; set; }
    public string Markdown { get; set; } = string.Empty;

    public JsonDocument? InkData { get; set; }
    public JsonDocument? Checklist { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? Priority { get; set; }
    public CardStatus Status { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    public Guid? Assignee { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? LastEditedBy { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class CardCreateDto
{
    public Guid BoardId { get; set; }

    public string? Title { get; set; }
    public string Markdown { get; set; } = string.Empty;

    public JsonDocument? InkData { get; set; }
    public JsonDocument? Checklist { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? Priority { get; set; }
    public CardStatus Status { get; set; } = CardStatus.open;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public Guid? Assignee { get; set; }
}

public sealed class CardUpdateDto
{
    public string? Title { get; set; }
    public string Markdown { get; set; } = string.Empty;

    public JsonDocument? InkData { get; set; }
    public JsonDocument? Checklist { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? Priority { get; set; }
    public CardStatus Status { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    public Guid? Assignee { get; set; }

    public int Version { get; set; }
}