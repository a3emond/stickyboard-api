using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.Attachments.Contracts;

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<IEnumerable<Attachment>> GetForCardAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<Attachment>> GetForBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Attachment>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);
}