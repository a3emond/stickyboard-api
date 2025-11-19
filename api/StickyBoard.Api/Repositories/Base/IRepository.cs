using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct);

        Task<Guid> CreateAsync(T entity, CancellationToken ct);
        Task<bool> UpdateAsync(T entity, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);

        Task<bool> ExistsAsync(Guid id, CancellationToken ct);

        Task<int> CountAsync(CancellationToken ct);
        Task<PagedResult<T>> GetPagedAsync(int limit, int offset, CancellationToken ct);
    }
}
