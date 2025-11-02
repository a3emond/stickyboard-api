using System.Security.Cryptography;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.BoardsAndCards;
using StickyBoard.Api.Repositories.Organizations;
using StickyBoard.Api.Repositories.SocialAndMessaging;
using StickyBoard.Api.Repositories.UsersAndAuth;

namespace StickyBoard.Api.Services;

public sealed class InviteService
{
    private readonly InviteRepository _invites;
    private readonly OrganizationMemberRepository _members;
    private readonly PermissionRepository _permissions;
    private readonly UserRelationRepository _relations;
    private readonly UserRepository _users;

    public InviteService(
        InviteRepository invites,
        OrganizationMemberRepository members,
        PermissionRepository permissions,
        UserRelationRepository relations,
        UserRepository users)
    {
        _invites = invites;
        _members = members;
        _permissions = permissions;
        _relations = relations;
        _users = users;
    }

    // ----------------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------------
    public async Task<InviteCreateResponseDto> CreateAsync(Guid senderId, InviteCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ValidationException("Email is required.");

        var email = dto.Email.Trim().ToLowerInvariant();
        var hasBoard = dto.BoardId.HasValue;
        var hasOrg = dto.OrgId.HasValue;

        if (hasBoard && hasOrg)
            throw new ValidationException("Cannot target both board and organization.");

        BoardRole? boardRole = dto.BoardRole;
        OrgRole? orgRole = dto.OrgRole;

        if (hasBoard && boardRole is null)
            throw new ValidationException("Board role required when inviting to a board.");

        if (hasOrg && orgRole is null)
            throw new ValidationException("Organization role required when inviting to an organization.");

        var token = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddDays(dto.ExpiresInDays.GetValueOrDefault(7));

        var invite = new Invite
        {
            SenderId = senderId,
            Email = email,
            BoardId = dto.BoardId,
            OrgId = dto.OrgId,
            BoardRole = boardRole,
            OrgRole = orgRole,
            Token = token,
            Accepted = false,
            ExpiresAt = expiresAt
        };

        var id = await _invites.CreateAsync(invite, ct);

        return new InviteCreateResponseDto
        {
            Id = id,
            Token = token,
            ExpiresAt = expiresAt
        };
    }


    // ----------------------------------------------------------------------
    // LOOKUP PUBLIC INVITE
    // ----------------------------------------------------------------------
    public async Task<InvitePublicDto?> GetPublicByTokenAsync(string token, CancellationToken ct)
    {
        var invite = await _invites.GetByTokenAsync(token, ct);
        if (invite is null) return null;

        var sender = await _users.GetByIdAsync(invite.SenderId, ct);
        return new InvitePublicDto
        {
            Email = invite.Email,
            BoardId = invite.BoardId,
            OrganizationId = invite.OrgId,
            BoardRole = invite.BoardRole,
            OrgRole = invite.OrgRole,
            Accepted = invite.Accepted,
            ExpiresAt = invite.ExpiresAt,
            SenderDisplayName = sender?.DisplayName ?? "Unknown"
        };
    }

    // ----------------------------------------------------------------------
    // REDEEM
    // ----------------------------------------------------------------------
    public async Task<bool> RedeemAsync(Guid userId, string token, CancellationToken ct)
    {
        var invite = await _invites.GetByTokenAsync(token, ct)
                     ?? throw new NotFoundException("Invite not found.");

        if (invite.Accepted || invite.ExpiresAt <= DateTime.UtcNow)
            throw new AuthExpiredException("Invite expired or already used.");

        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!string.Equals(user.Email?.Trim(), invite.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Invite email does not match current user.");

        if (invite.OrgId is not null)
        {
            await _members.CreateAsync(new OrganizationMember
            {
                OrgId = invite.OrgId.Value,
                UserId = userId,
                Role = invite.OrgRole ?? OrgRole.member
            }, ct);
        }
        else if (invite.BoardId is not null)
        {
            await _permissions.CreateAsync(new Permission
            {
                UserId = userId,
                BoardId = invite.BoardId.Value,
                Role = invite.BoardRole ?? BoardRole.viewer
            }, ct);
        }
        else
        {
            // friend invite
            await _relations.CreateAsync(new UserRelation
            {
                UserId = invite.SenderId,
                FriendId = userId,
                Status = RelationStatus.active_
            }, ct);
            await _relations.CreateAsync(new UserRelation
            {
                UserId = userId,
                FriendId = invite.SenderId,
                Status = RelationStatus.active_
            }, ct);
        }

        invite.Accepted = true;
        await _invites.UpdateAsync(invite, ct);
        return true;
    }

    // ----------------------------------------------------------------------
    // LIST PENDING
    // ----------------------------------------------------------------------
    public async Task<IEnumerable<InviteListItemDto>> GetPendingSentAsync(Guid senderId, CancellationToken ct)
    {
        var pending = await _invites.GetPendingBySenderAsync(senderId, ct);
        var sender = await _users.GetByIdAsync(senderId, ct);
        var senderName = sender?.DisplayName ?? "You";

        return pending.Select(i => MapListItem(i, senderName));
    }

    public async Task<IEnumerable<InviteListItemDto>> GetPendingForUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        var items = await _invites.GetPendingForEmailAsync(user.Email.Trim().ToLowerInvariant(), ct);
        var list = new List<InviteListItemDto>();

        foreach (var i in items)
        {
            var sender = await _users.GetByIdAsync(i.SenderId, ct);
            list.Add(MapListItem(i, sender?.DisplayName ?? "Unknown"));
        }

        return list;
    }

    // ----------------------------------------------------------------------
    // CANCEL
    // ----------------------------------------------------------------------
    public async Task<bool> CancelAsync(Guid senderId, Guid inviteId, CancellationToken ct)
    {
        var ok = await _invites.CancelIfOwnedAsync(senderId, inviteId, ct);
        if (!ok) throw new NotFoundException("Invite not found or not owned by user.");
        return true;
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------
    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static InviteListItemDto MapListItem(Invite i, string senderName) => new()
    {
        Id = i.Id,
        Email = i.Email,
        BoardId = i.BoardId,
        OrganizationId = i.OrgId,
        BoardRole = i.BoardRole,
        OrgRole = i.OrgRole,
        Accepted = i.Accepted,
        CreatedAt = i.CreatedAt,
        ExpiresAt = i.ExpiresAt,
        SenderDisplayName = senderName
    };
}
