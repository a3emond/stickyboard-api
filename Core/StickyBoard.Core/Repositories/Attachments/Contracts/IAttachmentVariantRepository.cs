using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.Attachments.Contracts;

public interface IAttachmentVariantRepository : IRepository<AttachmentVariant>
{
    Task<IEnumerable<AttachmentVariant>> GetForParentAsync(Guid parentId, CancellationToken ct);
}