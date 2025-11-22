using StickyBoard.Api.DTOs.Attachments;

namespace StickyBoard.Api.Services.Attachments.Contracts;

public interface IAttachmentService
{
    Task<AttachmentDto> CreateAsync(Guid uploaderId, AttachmentCreateDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid id, AttachmentUpdateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    Task<IEnumerable<AttachmentDto>> GetForCardAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<AttachmentDto>> GetForBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<AttachmentDto>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);
    Task<AttachmentDto?> GetAsync(Guid id, CancellationToken ct);
}