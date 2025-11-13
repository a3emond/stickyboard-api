using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs.SocialAndMessaging;

public sealed class InviteCreateRequestDto
{
    public Guid SenderId { get; set; }
    public string Email { get; set; } = string.Empty;

    public InviteScope Scope { get; set; }

    public Guid? WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? ContactId { get; set; }

    public WorkspaceRole? TargetRole { get; set; }
    public WorkspaceRole? BoardRole { get; set; }

    public TimeSpan ExpiresIn { get; set; } = TimeSpan.FromDays(7);
    public string? Note { get; set; }
}

public sealed class InviteCreateResponseDto
{
    public Guid InviteId { get; set; }
    public string InviteUrl { get; set; } = string.Empty;
}

public sealed class InviteAcceptRequestDto
{
    public string Token { get; set; } = string.Empty;
    public Guid AcceptingUserId { get; set; }
}

public sealed class InviteAcceptResponseDto
{
    public Guid InviteId { get; set; }
    public InviteScope Scope { get; set; }

    public Guid? WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? ContactId { get; set; }

    public WorkspaceRole? TargetRole { get; set; }
    public WorkspaceRole? BoardRole { get; set; }
}

public sealed class InviteDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string Email { get; set; } = string.Empty;
    public InviteScope ScopeType { get; set; }

    public Guid? WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? ContactId { get; set; }

    public WorkspaceRole? TargetRole { get; set; }
    public WorkspaceRole? BoardRole { get; set; }

    public InviteStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Guid? AcceptedBy { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string? Note { get; set; }
}

public sealed class InviteRevokeRequestDto
{
    public string Token { get; set; } = string.Empty;
}