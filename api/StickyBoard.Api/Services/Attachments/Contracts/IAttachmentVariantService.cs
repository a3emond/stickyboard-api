using StickyBoard.Api.DTOs.Attachments;

namespace StickyBoard.Api.Services.Attachments.Contracts;

public interface IAttachmentVariantService
{
    // Used by workers
    Task<AttachmentVariantDto> CreateAsync(AttachmentVariantCreateDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid id, AttachmentVariantUpdateDto dto, CancellationToken ct);

    // Used by clients
    Task<IEnumerable<AttachmentVariantDto>> GetForParentAsync(Guid parentId, CancellationToken ct);
    Task<AttachmentVariantDto?> GetAsync(Guid parentId, string variant, CancellationToken ct);
}