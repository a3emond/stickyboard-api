using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Services.Attachments;

public interface IAttachmentManager
{
    Task<AttachmentInitResult> InitUploadAsync(Guid userId, AttachmentInitRequest request, CancellationToken ct);
    Task MarkUploadFailedAsync(Guid attachmentId, CancellationToken ct);
    Task MarkUploadCompleteAsync(Guid attachmentId, CancellationToken ct);

    Task<AttachmentDownloadResult> GetDownloadUrlAsync(Guid attachmentId, string? variant, CancellationToken ct);

    Task<IReadOnlyList<AttachmentMetaDto>> GetForCardAsync(Guid cardId, CancellationToken ct);
    Task<IReadOnlyList<AttachmentMetaDto>> GetForBoardAsync(Guid boardId, CancellationToken ct);
    Task<IReadOnlyList<AttachmentMetaDto>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);

    Task<string> GetOriginalForProcessingAsync(Guid attachmentId, CancellationToken ct);
    Task<string> GetVariantUploadUrlAsync(Guid attachmentId, AttachmentVariantType variant, CancellationToken ct);

    Task RegisterVariantAsync(VariantRegisterRequest request, CancellationToken ct);

    Task<bool> DeleteAsync(Guid attachmentId, CancellationToken ct);
    Task RevokeAllTokensAsync(Guid attachmentId, CancellationToken ct);
}