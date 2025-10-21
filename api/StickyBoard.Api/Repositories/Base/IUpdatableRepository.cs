using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base
{
    public interface IUpdatableRepository<T> : IRepository<T> where T : class, IEntityUpdatable
    {
        // future hooks for versioning or audit 
    }
}