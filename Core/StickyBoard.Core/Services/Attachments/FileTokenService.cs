using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Services.Attachments.Contracts;

namespace StickyBoard.Core.Services.Attachments;

public sealed class FileTokenService : IFileTokenService
{
    private readonly IFileTokenRepository _tokens;

    public FileTokenService(IFileTokenRepository tokens)
    {
        _tokens = tokens;
    }

    // ------------------------------------------------------------
    // CREATE (DB only)
    // ------------------------------------------------------------
    public async Task<FileTokenDto> CreateAsync(Guid userId, FileTokenCreateDto dto, CancellationToken ct)
    {
        var e = new FileToken
        {
            AttachmentId = dto.AttachmentId,
            Variant       = dto.Variant,
            Secret        = null,         // No secrets stored here anymore
            Audience      = dto.Audience,
            ExpiresAt     = dto.ExpiresAt ?? DateTime.UtcNow.AddHours(1),
            CreatedBy     = userId,
            Revoked       = false
        };

        var id = await _tokens.CreateAsync(e, ct);
        var created = await _tokens.GetByIdAsync(id, ct);

        return Map(created!);
    }

    // ------------------------------------------------------------
    // READ
    // ------------------------------------------------------------
    public async Task<IEnumerable<FileTokenDto>> GetValidForAttachmentAsync(
        Guid attachmentId,
        CancellationToken ct)
    {
        var list = await _tokens.GetValidForAttachmentAsync(
            attachmentId,
            DateTime.UtcNow,
            ct);

        return list.Select(Map);
    }

    // ------------------------------------------------------------
    // REVOKE
    // ------------------------------------------------------------
    public Task<bool> RevokeAsync(Guid id, CancellationToken ct)
        => _tokens.RevokeAsync(id, ct);

    public Task<int> RevokeAllForAttachmentAsync(Guid attachmentId, CancellationToken ct)
        => _tokens.RevokeAllForAttachmentAsync(attachmentId, ct);

    // ------------------------------------------------------------
    // MAP
    // ------------------------------------------------------------
    private static FileTokenDto Map(FileToken t)
    {
        return new FileTokenDto
        {
            Id           = t.Id,
            AttachmentId = t.AttachmentId,
            Variant      = t.Variant,
            Audience     = t.Audience,
            ExpiresAt    = t.ExpiresAt,
            CreatedBy    = t.CreatedBy,
            Revoked      = t.Revoked,
            CreatedAt    = t.CreatedAt
        };
    }
}
