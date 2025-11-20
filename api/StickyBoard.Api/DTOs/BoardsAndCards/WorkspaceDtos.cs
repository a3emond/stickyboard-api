using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.BoardsAndCards;

// ------------------------------------------------------------
// READ
// ------------------------------------------------------------
public sealed class WorkspaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class WorkspaceMemberDto
{
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}


// ------------------------------------------------------------
// WRITE
// ------------------------------------------------------------
public sealed class WorkspaceCreateDto
{
    public string Name { get; set; } = string.Empty;
}

// ------------------------------------------------------------
// RENAME
// ------------------------------------------------------------
public sealed class WorkspaceRenameDto
{
    public string Name { get; set; } = string.Empty;
}

// ------------------------------------------------------------
// ADD MEMBER
// ------------------------------------------------------------
public sealed class WorkspaceAddMemberDto
{
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.member;
}