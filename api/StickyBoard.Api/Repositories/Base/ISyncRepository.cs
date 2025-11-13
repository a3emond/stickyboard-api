namespace StickyBoard.Api.Repositories.Base;

public interface ISyncRepository<T>
{
    Task<IEnumerable<T>> GetUpdatedSinceAsync(DateTime since, CancellationToken ct);

    Task<PagedResult<T>> GetUpdatedSincePagedAsync(
        DateTime since, int limit, int offset, CancellationToken ct);
}