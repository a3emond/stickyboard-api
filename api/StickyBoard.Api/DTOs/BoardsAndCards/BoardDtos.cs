using System.Text.Json;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.BoardsAndCards;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class BoardDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;

    public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument Meta  { get; set; } = JsonDocument.Parse("{}");

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class BoardMemberDto
{
    public Guid UserId { get; set; }
    public WorkspaceRole? Role { get; set; }
}

// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class BoardCreateDto
{
    public string Title { get; set; } = string.Empty;

    public JsonDocument? Theme { get; set; }
    public JsonDocument? Meta  { get; set; }
}

public sealed class BoardRenameDto
{
    public string Title { get; set; } = string.Empty;
}

public sealed class BoardAddMemberDto
{
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.member;
}