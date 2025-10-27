using System.Security.Cryptography;
using System.Text;
using StickyBoard.Api.DTOs.Messaging;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Messaging;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Services;
using StickyBoard.Api.Models.Social;

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

            // Validation: exactly one of friend | board | org paths
            var isFriendInvite = dto.BoardId is null && dto.OrganizationId is null;
            if (!isFriendInvite)
            {
                if (dto.BoardId is null && dto.OrganizationId is null)
                    throw new ArgumentException("Invalid invite target.");
                if (string.IsNullOrWhiteSpace(dto.Role))
                    throw new ArgumentException("Role is required for board/org invites.");
            }

            var token = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddDays(dto.ExpiresInDays.GetValueOrDefault(7));
            BoardRole? boardRole = null;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (Enum.TryParse<BoardRole>(dto.Role, true, out var br))
                    boardRole = br;
                else
                    throw new ArgumentException("Invalid role.");
            }

            var invite = new Invite
            {
                Id = Guid.Empty, // will be returned by repo
                SenderId = senderId,
                Email = email,
                BoardId = dto.BoardId,
                OrganizationId = dto.OrganizationId,
                Role = boardRole,
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
        // PUBLIC LOOKUP FOR LANDING PAGE
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
                OrganizationId = invite.OrganizationId,
                Role = invite.Role?.ToString(),
                ExpiresAt = invite.ExpiresAt,
                Accepted = invite.Accepted,
                SenderDisplayName = sender?.DisplayName ?? "Unknown"
            };
            // (Do not expose sender email here)
        }

        // ------------------------------------------------------------
        // REDEEM
        // ------------------------------------------------------------
        public async Task<bool> RedeemAsync(Guid userId, string token, CancellationToken ct)
        {
            var invite = await _invites.GetByTokenAsync(token, ct);
            if (invite is null || invite.Accepted || invite.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired invite.");

            // Idempotency: user email must match invite email (if user exists)
            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null) throw new InvalidOperationException("User not found.");
            if (!string.Equals(user.Email?.Trim(), invite.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invite email does not match current user.");

            // 1) Organization membership
            if (invite.OrganizationId is not null)
            {
                var role = invite.Role.HasValue ? (OrgRole)invite.Role.Value : OrgRole.member;
                await _members.CreateAsync(new OrganizationMember
                {
                    OrganizationId = invite.OrganizationId.Value,
                    UserId = userId,
                    Role = role
                }, ct);
            }

            // 2) Board permission
            if (invite.BoardId is not null)
            {
                await _permissions.CreateAsync(new Permission
                {
                    UserId = userId,
                    BoardId = invite.BoardId.Value,
                    Role = invite.Role ?? BoardRole.viewer
                }, ct);
            }

            // 3) Friend relation (pure email invite)
            if (invite.OrganizationId is null && invite.BoardId is null)
            {
                // Two directional rows
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

            // Mark accepted
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
            var list = new List<InviteListItemDto>();
            foreach (var i in pending)
            {
                list.Add(new InviteListItemDto
                {
                    Id = i.Id,
                    Email = i.Email,
                    BoardId = i.BoardId,
                    OrganizationId = i.OrganizationId,
                    Role = i.Role?.ToString(),
                    Accepted = i.Accepted,
                    CreatedAt = i.CreatedAt,
                    ExpiresAt = i.ExpiresAt,
                    SenderDisplayName = sender?.DisplayName ?? "You"
                });
            }
            return list;
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
                list.Add(new InviteListItemDto
                {
                    Id = i.Id,
                    Email = i.Email,
                    BoardId = i.BoardId,
                    OrganizationId = i.OrganizationId,
                    Role = i.Role?.ToString(),
                    Accepted = i.Accepted,
                    CreatedAt = i.CreatedAt,
                    ExpiresAt = i.ExpiresAt,
                    SenderDisplayName = sender?.DisplayName ?? "Unknown"
                });
            }
            return list;
        }

        // ------------------------------------------------------------
        // CANCEL (sender-only)
        // ------------------------------------------------------------
        public async Task<bool> CancelAsync(Guid senderId, Guid inviteId, CancellationToken ct)
        {
            return await _invites.CancelIfOwnedAsync(senderId, inviteId, ct);
        }


        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        private static string GenerateToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
        {
            var s = Convert.ToBase64String(bytes);
            return s.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
