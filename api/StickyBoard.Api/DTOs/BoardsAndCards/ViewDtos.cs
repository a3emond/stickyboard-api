using System.Text.Json;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.BoardsAndCards;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class ViewDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }

    public string Title { get; set; } = string.Empty;
    public ViewType Type { get; set; }

    public JsonDocument Layout { get; set; } = JsonDocument.Parse("{}");

    public int Position { get; set; }
    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class ViewCreateDto
{
    public string Title { get; set; } = string.Empty;
    public ViewType Type { get; set; }

    public JsonDocument? Layout { get; set; }

    public int Position { get; set; }
}

public sealed class ViewUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public ViewType Type { get; set; }

    public JsonDocument? Layout { get; set; }

    public int Position { get; set; }
    public int Version { get; set; }   // REQUIRED for optimistic concurrency
}