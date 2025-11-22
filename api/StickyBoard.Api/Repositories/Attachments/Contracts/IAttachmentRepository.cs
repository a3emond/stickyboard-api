using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments.Contracts;

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<IEnumerable<Attachment>> GetForCardAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<Attachment>> GetForBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Attachment>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);
}