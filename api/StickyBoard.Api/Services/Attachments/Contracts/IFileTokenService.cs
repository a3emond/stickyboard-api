using StickyBoard.Api.DTOs.Attachments;

namespace StickyBoard.Api.Services.Attachments.Contracts;

public interface IFileTokenService
{
    Task<FileTokenDto> CreateAsync(Guid userId, FileTokenCreateDto dto, CancellationToken ct);
    Task<IEnumerable<FileTokenDto>> GetValidForAttachmentAsync(Guid attachmentId, CancellationToken ct);
    Task<bool> RevokeAsync(Guid id, CancellationToken ct);
    Task<int> RevokeAllForAttachmentAsync(Guid attachmentId, CancellationToken ct);
}