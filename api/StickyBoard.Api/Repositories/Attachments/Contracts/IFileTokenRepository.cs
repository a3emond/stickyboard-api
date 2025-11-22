using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments.Contracts;

public interface IFileTokenRepository : IRepository<FileToken>
{
    Task<IEnumerable<FileToken>> GetValidForAttachmentAsync(Guid attachmentId, DateTime now, CancellationToken ct);
    Task<bool> RevokeAsync(Guid tokenId, CancellationToken ct);
    Task<int> RevokeAllForAttachmentAsync(Guid attachmentId, CancellationToken ct);
}