using System.Security.Cryptography;
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

    public async Task<FileTokenDto> CreateAsync(Guid userId, FileTokenCreateDto dto, CancellationToken ct)
    {
        var secret = new byte[32];
        RandomNumberGenerator.Fill(secret);

        var expires = dto.ExpiresAt ?? DateTime.UtcNow.AddHours(1);

        var e = new FileToken
        {
            AttachmentId = dto.AttachmentId,
            Variant = dto.Variant,
            Secret = secret,
            Audience = dto.Audience,
            ExpiresAt = expires,
            CreatedBy = userId,
            Revoked = false
        };

        var id = await _tokens.CreateAsync(e, ct);
        var created = await _tokens.GetByIdAsync(id, ct);

        return Map(created!);
    }

    public async Task<IEnumerable<FileTokenDto>> GetValidForAttachmentAsync(Guid attachmentId, CancellationToken ct)
    {
        var list = await _tokens.GetValidForAttachmentAsync(attachmentId, DateTime.UtcNow, ct);
        return list.Select(Map);
    }

    public Task<bool> RevokeAsync(Guid id, CancellationToken ct)
    {
        return _tokens.RevokeAsync(id, ct);
    }

    public Task<int> RevokeAllForAttachmentAsync(Guid attachmentId, CancellationToken ct)
    {
        return _tokens.RevokeAllForAttachmentAsync(attachmentId, ct);
    }

    private static FileTokenDto Map(FileToken t)
    {
        return new FileTokenDto
        {
            Id = t.Id,
            AttachmentId = t.AttachmentId,
            Variant = t.Variant,
            Audience = t.Audience,
            ExpiresAt = t.ExpiresAt,
            CreatedBy = t.CreatedBy,
            Revoked = t.Revoked,
            CreatedAt = t.CreatedAt
        };
    }
}