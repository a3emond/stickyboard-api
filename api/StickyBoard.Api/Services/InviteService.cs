using System.Security.Cryptography;
using StickyBoard.Api.DTOs.Messaging;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Models.Social;
using StickyBoard.Api.Repositories;

namespace StickyBoard.Api.Services
{
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

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public async Task<InviteCreateResponseDto> CreateAsync(Guid senderId, InviteCreateDto dto, CancellationToken ct)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            var hasBoard = dto.BoardId.HasValue;
            var hasOrg = dto.OrganizationId.HasValue;
            var isFriendInvite = !hasBoard && !hasOrg;

            // Must be exactly one target or none (friend)
            if (hasBoard && hasOrg)
                throw new ArgumentException("Cannot target both board and organization.");
            
            // Parse roles depending on target
            BoardRole? boardRole = null;
            OrgRole? orgRole = null;

            if (hasBoard)
            {
                if (string.IsNullOrWhiteSpace(dto.BoardRole))
                    throw new ArgumentException("Board role is required for board invites.");
                if (!Enum.TryParse(dto.BoardRole, true, out BoardRole parsed))
                    throw new ArgumentException("Invalid board role.");
                boardRole = parsed;
            }
            else if (hasOrg)
            {
                if (string.IsNullOrWhiteSpace(dto.OrgRole))
                    throw new ArgumentException("Organization role is required for org invites.");
                if (!Enum.TryParse(dto.OrgRole, true, out OrgRole parsed))
                    throw new ArgumentException("Invalid organization role.");
                orgRole = parsed;
            }

            var token = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddDays(dto.ExpiresInDays.GetValueOrDefault(7));

            var invite = new Invite
            {
                Id = Guid.Empty,
                SenderId = senderId,
                Email = email,
                BoardId = dto.BoardId,
                OrgId = dto.OrganizationId,
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

        // ------------------------------------------------------------
        // PUBLIC LOOKUP
        // ------------------------------------------------------------
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
                BoardRole = invite.BoardRole?.ToString(),
                OrgRole = invite.OrgRole?.ToString(),
                ExpiresAt = invite.ExpiresAt,
                Accepted = invite.Accepted,
                SenderDisplayName = sender?.DisplayName ?? "Unknown"
            };
        }

        // ------------------------------------------------------------
        // REDEEM
        // ------------------------------------------------------------
        public async Task<bool> RedeemAsync(Guid userId, string token, CancellationToken ct)
        {
            var invite = await _invites.GetByTokenAsync(token, ct);
            if (invite is null || invite.Accepted || invite.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired invite.");

            var user = await _users.GetByIdAsync(userId, ct)
                       ?? throw new InvalidOperationException("User not found.");

            if (!string.Equals(user.Email?.Trim(), invite.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invite email does not match current user.");

            if (invite.OrgId is not null)
            {
                await _members.CreateAsync(new OrganizationMember
                {
                    OrganizationId = invite.OrgId.Value,
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
                await _relations.CreateAsync(new UserRelation
                {
                    UserId = invite.SenderId,
                    FriendId = userId,
                    Status = RelationStatus.active
                }, ct);

                await _relations.CreateAsync(new UserRelation
                {
                    UserId = userId,
                    FriendId = invite.SenderId,
                    Status = RelationStatus.active
                }, ct);
            }

            invite.Accepted = true;
            await _invites.UpdateAsync(invite, ct);

            return true;
        }

        // ------------------------------------------------------------
        // LISTS
        // ------------------------------------------------------------
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
                       ?? throw new InvalidOperationException("User not found.");

            var items = await _invites.GetPendingForEmailAsync(user.Email.Trim().ToLowerInvariant(), ct);
            var list = new List<InviteListItemDto>();

            foreach (var i in items)
            {
                var sender = await _users.GetByIdAsync(i.SenderId, ct);
                list.Add(MapListItem(i, sender?.DisplayName ?? "Unknown"));
            }

            return list;
        }

        // ------------------------------------------------------------
        // CANCEL
        // ------------------------------------------------------------
        public Task<bool> CancelAsync(Guid senderId, Guid inviteId, CancellationToken ct)
            => _invites.CancelIfOwnedAsync(senderId, inviteId, ct);

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        private static string GenerateToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
        // -------------------- Mapping --------------------
        private static InviteListItemDto MapListItem(Invite i, string senderName) => new()
        {
            Id = i.Id,
            Email = i.Email,
            BoardId = i.BoardId,
            OrganizationId = i.OrgId,
            BoardRole = i.BoardRole?.ToString(),
            OrgRole = i.OrgRole?.ToString(),
            Accepted = i.Accepted,
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt,
            SenderDisplayName = senderName
        };

    }
}
