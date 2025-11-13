using System.Security.Cryptography;
using System.Text;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class InviteService : IInviteService
{
    private readonly IInviteRepository _repo;
    private readonly IPasswordHasher _hasher;

    public InviteService(IInviteRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    // ------------------------------------------------------------
    // Token generator (same entropy as refresh tokens)
    // ------------------------------------------------------------
    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32]; // 256-bit random
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public async Task<InviteCreateResponseDto> CreateAsync(
        InviteCreateRequestDto dto,
        CancellationToken ct)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = _hasher.HashToken(rawToken);

        var id = await _repo.CreateViaDbFunctionAsync(
            dto.SenderId,
            dto.Email,
            dto.Scope,
            dto.WorkspaceId,
            dto.BoardId,
            dto.ContactId,
            dto.TargetRole,
            dto.BoardRole,
            tokenHash,
            dto.ExpiresIn,
            dto.Note,
            ct
        );

        // Build public URL (canonical)
        // This is the primary invite entrypoint used in emails and cross-platform links.
        // The web app at /invite handles:
        //   - token verification
        //   - login/signup if needed
        //   - invite acceptance
        //   - optional redirect to platform downloads (iOS/Android/Desktop)
        string inviteUrl = InviteUrlBuilder.BuildInviteUrl(rawToken);

        /*
            Other URL variants available if needed:

            1) Deep Link (handled directly by mobile apps)
               Opens the StickyBoard app immediately if installed.
               Example: stickyboard://invite?token=...

                 var deepLink = InviteUrlBuilder.BuildDeepLink(rawToken);

            2) Mobile Landing Page
               Optional page when you want a nicer experience on mobile:
               - detects platform
               - redirects to AppStore / PlayStore if app not installed

                 var mobileUrl = InviteUrlBuilder.BuildMobileLandingPageUrl(rawToken);

            3) Future custom endpoints (examples)
               - Download stickyboard.aedev.pro/download?inviteToken=...
               - Desktop installer page
               - Invitation preview screen

            NOTE:
            Only RAW token is ever exposed to the client.
            Database stores TokenHash = SHA256(rawToken).
        */
        
        return new InviteCreateResponseDto
        {
            InviteId = id,
            InviteUrl = inviteUrl
        };
    }

    // ------------------------------------------------------------
    // ACCEPT
    // ------------------------------------------------------------
    public async Task<InviteAcceptResponseDto?> AcceptAsync(
        InviteAcceptRequestDto dto,
        CancellationToken ct)
    {
        var hash = _hasher.HashToken(dto.Token);

        var result = await _repo.AcceptViaDbFunctionAsync(hash, dto.AcceptingUserId, ct);
        if (result == null) return null;

        return new InviteAcceptResponseDto
        {
            InviteId = result.InviteId,
            Scope = result.Scope,
            WorkspaceId = result.WorkspaceId,
            BoardId = result.BoardId,
            ContactId = result.ContactId,
            TargetRole = result.TargetRole,
            BoardRole = result.BoardRole
        };
    }

    // ------------------------------------------------------------
    // REVOKE
    // ------------------------------------------------------------
    public async Task<bool> RevokeAsync(string token, CancellationToken ct)
    {
        var hash = _hasher.HashToken(token);
        return await _repo.RevokeViaDbFunctionAsync(hash, ct);
    }

    // ------------------------------------------------------------
    // LISTING & LOOKUPS
    // ------------------------------------------------------------
    public async Task<IEnumerable<InviteDto>> GetBySenderAsync(Guid senderId, CancellationToken ct)
    {
        var items = await _repo.GetBySenderAsync(senderId, ct);
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<InviteDto>> GetByEmailAsync(string email, CancellationToken ct)
    {
        var items = await _repo.GetByEmailAsync(email, ct);
        return items.Select(MapToDto);
    }

    public async Task<InviteDto?> GetByTokenAsync(string token, CancellationToken ct)
    {
        var hash = _hasher.HashToken(token);
        var invite = await _repo.GetByTokenHashAsync(hash, ct);
        return invite == null ? null : MapToDto(invite);
    }

    // ------------------------------------------------------------
    // Mapping
    // ------------------------------------------------------------
    private static InviteDto MapToDto(Invite e)
    {
        return new InviteDto
        {
            Id = e.Id,
            SenderId = e.SenderId,
            Email = e.Email,
            ScopeType = e.ScopeType,

            WorkspaceId = e.WorkspaceId,
            BoardId = e.BoardId,
            ContactId = e.ContactId,

            TargetRole = e.TargetRole,
            BoardRole = e.BoardRole,

            Status = e.Status,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt,

            AcceptedBy = e.AcceptedBy,
            AcceptedAt = e.AcceptedAt,
            RevokedAt = e.RevokedAt,

            Note = e.Note
        };
    }
}
