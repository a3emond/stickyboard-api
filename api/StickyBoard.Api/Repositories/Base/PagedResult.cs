namespace StickyBoard.Api.Repositories.Base;

public sealed class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int Total { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }

    public static PagedResult<T> Create(IEnumerable<T> items, int total, int limit, int offset)
        => new() { Items = items, Total = total, Limit = limit, Offset = offset };
    
    public static PagedResult<T> Empty(int limit, int offset)
        => new() { Items = Enumerable.Empty<T>(), Total = 0, Limit = limit, Offset = offset };

}