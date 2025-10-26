using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Models.Messaging;

namespace StickyBoard.Api.Services;

public sealed class InviteService
{
    private readonly InviteRepository _invites;
    private readonly OrganizationMemberRepository _members;
    private readonly PermissionRepository _permissions;

    public InviteService(InviteRepository invites,
        OrganizationMemberRepository members,
        PermissionRepository permissions)
    {
        _invites = invites;
        _members = members;
        _permissions = permissions;
    }

    public async Task<bool> RedeemInviteAsync(Guid userId, string token, CancellationToken ct)
    {
        var invite = await _invites.GetByTokenAsync(token, ct);
        if (invite is null || invite.Accepted || invite.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired invite.");

        // Organization membership
        if (invite.OrganizationId is not null)
        {
            await _members.CreateAsync(new OrganizationMember
            {
                OrganizationId = invite.OrganizationId.Value,
                UserId = userId,
                Role = invite.Role.HasValue ? (OrgRole)invite.Role.Value : OrgRole.member
            }, ct);
        }

        // Board permission
        if (invite.BoardId is not null)
        {
            await _permissions.CreateAsync(new Permission
            {
                UserId = userId,
                BoardId = invite.BoardId.Value,
                Role = invite.Role ?? BoardRole.viewer
            }, ct);
        }

        // Mark accepted
        invite.Accepted = true;
        await _invites.UpdateAsync(invite, ct);
        return true;
    }
}
