using System.Security.Cryptography;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;
using StickyBoard.Api.Auth;

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

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ---------------------------------------------------------------------
    // CREATE  (no URL generation here!)
    // ---------------------------------------------------------------------
    public async Task<InviteCreateResponseDto> CreateAsync(
        InviteCreateRequestDto dto,
        CancellationToken ct)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = _hasher.HashToken(rawToken);

        var inviteId = await _repo.CreateViaDbFunctionAsync(
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

        // Worker will receive:
        // - invite.created outbox entry
        // - inviteId
        // - tokenHash (from DB)
        // And will regenerate:
        // - final public invite URL
        // - email / firebase notification payloads

        return new InviteCreateResponseDto
        {
            InviteId = inviteId,
            InviteToken = rawToken  // returned as "token" for the worker/email
        };
    }

    // ---------------------------------------------------------------------
    // ACCEPT
    // ---------------------------------------------------------------------
    public async Task<InviteAcceptResponseDto?> AcceptAsync(
        InviteAcceptRequestDto dto,
        CancellationToken ct)
    {
        var hash = _hasher.HashToken(dto.Token);

        var result = await _repo.AcceptViaDbFunctionAsync(hash, dto.AcceptingUserId, ct);
        if (result == null)
            return null;

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

    // ---------------------------------------------------------------------
    // REVOKE
    // ---------------------------------------------------------------------
    public async Task<bool> RevokeAsync(string token, CancellationToken ct)
    {
        var hash = _hasher.HashToken(token);
        return await _repo.RevokeViaDbFunctionAsync(hash, ct);
    }

    // ---------------------------------------------------------------------
    // LOOKUPS
    // ---------------------------------------------------------------------
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
