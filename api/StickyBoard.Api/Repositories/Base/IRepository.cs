using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<Guid> CreateAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
    }
}