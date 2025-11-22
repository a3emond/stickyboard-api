using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments.Contracts;

public interface IAttachmentVariantRepository : IRepository<AttachmentVariant>
{
    Task<IEnumerable<AttachmentVariant>> GetForParentAsync(Guid parentId, CancellationToken ct);
}