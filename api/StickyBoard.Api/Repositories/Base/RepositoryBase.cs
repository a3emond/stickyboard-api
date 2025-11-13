using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Npgsql;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base;
/*
    ============================================================
    RepositoryBase<T> Capabilities
    ------------------------------------------------------------
    This repository inherits the full CRUD + Sync foundation
    implemented in RepositoryBase<T>. The base class provides:

    1. Table Resolution
       - Automatically detects the table name from [Table] attribute
         or falls back to typeof(T).Name.ToLowerInvariant().

    2. Connection Handling
       - OpenConnectionAsync wrapper using NpgsqlDataSource.

    3. Entity Mapping
       - Abstract Map(NpgsqlDataReader) method that child classes
         must implement.
       - MapListAsync(...) helper to map multiple rows.

    4. Soft Delete Support
       - If T implements ISoftDeletable, all SELECT queries 
         automatically add "deleted_at IS NULL".
       - DeleteAsync() performs UPDATE deleted_at = NOW()
         instead of a hard delete.

    5. Read Operations
       - GetByIdAsync(Guid)
       - GetAllAsync()
       - ExistsAsync(Guid)
       - CountAsync()
       - GetPagedAsync(limit, offset):
           Paging ordered by updated_at DESC.

    6. Sync Operations
       - GetUpdatedSinceAsync(DateTime since):
           Returns all rows updated after the given timestamp.
       - GetUpdatedSincePagedAsync(DateTime since, limit, offset):
           Paged sync results with count + ordering.

    7. Write Operations (abstract, implemented per repository)
       - CreateAsync(T entity)
       - UpdateAsync(T entity)

    8. Delete Operations
       - DeleteAsync(id):
            Soft-delete if entity implements ISoftDeletable,
            otherwise performs hard delete.

    Notes for inheriting repositories:
    -----------------------------------
    • You MUST implement Map(), CreateAsync(), and UpdateAsync().
    • All SELECT statements automatically integrate soft-delete logic.
    • Paging and sync helpers are fully reusable as-is.
    • Write logic is intentionally left abstract to allow entity-
      specific INSERT / UPDATE SQL.
    • This base class is designed for simple, fast, fully explicit 
      SQL without ORM abstraction, and keeps behaviour consistent 
      across all repositories.
    ============================================================
*/


public abstract class RepositoryBase<T> : IRepository<T>, ISyncRepository<T>
    where T : class, IEntity, new()
{
    private readonly NpgsqlDataSource _db;

    protected RepositoryBase(NpgsqlDataSource db) => _db = db;

    protected string Table =>
        typeof(T).GetCustomAttribute<TableAttribute>()?.Name
        ?? typeof(T).Name.ToLowerInvariant();

    // -------------------------------------------
    // Connection
    // -------------------------------------------
    protected ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
        => _db.OpenConnectionAsync(ct);

    // -------------------------------------------
    // Mapping
    // -------------------------------------------
    protected abstract T Map(NpgsqlDataReader r);

    protected async Task<List<T>> MapListAsync(NpgsqlDataReader r, CancellationToken ct)
    {
        var list = new List<T>();
        while (await r.ReadAsync(ct)) list.Add(Map(r));
        return list;
    }

    // -------------------------------------------
    // Soft Delete helper
    // -------------------------------------------
    protected string WithSoftDelete(string sql)
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            return sql;

        return sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
            ? sql + " AND deleted_at IS NULL"
            : sql + " WHERE deleted_at IS NULL";
    }

    // -------------------------------------------
    // Get by Id
    // -------------------------------------------
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var sql = WithSoftDelete($"SELECT * FROM {Table} WHERE id = @id");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    // -------------------------------------------
    // Get all
    // -------------------------------------------
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
    {
        var sql = WithSoftDelete($"SELECT * FROM {Table}");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        return await MapListAsync(r, ct);
    }

    // -------------------------------------------
    // Exists
    // -------------------------------------------
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        var sql = WithSoftDelete($"SELECT 1 FROM {Table} WHERE id = @id");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("id", id);

        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    // -------------------------------------------
    // Count
    // -------------------------------------------
    public async Task<int> CountAsync(CancellationToken ct)
    {
        var sql = WithSoftDelete($"SELECT COUNT(*) FROM {Table}");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    // -------------------------------------------
    // Paging
    // -------------------------------------------
    public async Task<PagedResult<T>> GetPagedAsync(int limit, int offset, CancellationToken ct)
    {
        var sql = WithSoftDelete($@"
            SELECT * FROM {Table}
            ORDER BY updated_at DESC
            LIMIT @limit OFFSET @offset");

        var count = await CountAsync(ct);

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var items = await MapListAsync(r, ct);

        return PagedResult<T>.Create(items, count, limit, offset);
    }

    // -------------------------------------------
    // Sync
    // -------------------------------------------
    public async Task<IEnumerable<T>> GetUpdatedSinceAsync(DateTime since, CancellationToken ct)
    {
        var sql = WithSoftDelete(
            $"SELECT * FROM {Table} WHERE updated_at > @since");

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("since", since);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<PagedResult<T>> GetUpdatedSincePagedAsync(
        DateTime since, int limit, int offset, CancellationToken ct)
    {
        var countSql = WithSoftDelete($@"
            SELECT COUNT(*) FROM {Table}
            WHERE updated_at > @since");

        var sql = WithSoftDelete($@"
            SELECT * FROM {Table}
            WHERE updated_at > @since
            ORDER BY updated_at DESC
            LIMIT @limit OFFSET @offset");

        await using var c = await Conn(ct);

        // Count
        await using var cmdCount = new NpgsqlCommand(countSql, c);
        cmdCount.Parameters.AddWithValue("since", since);
        var count = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

        // Page
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("since", since);
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var items = await MapListAsync(r, ct);

        return PagedResult<T>.Create(items, count, limit, offset);
    }

    // -------------------------------------------
    // Abstract writes
    // -------------------------------------------
    public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var c = await Conn(ct);

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            var sql = $"UPDATE {Table} SET deleted_at = NOW() WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        var hard = $"DELETE FROM {Table} WHERE id = @id";
        await using var cmdHard = new NpgsqlCommand(hard, c);
        cmdHard.Parameters.AddWithValue("id", id);
        return await cmdHard.ExecuteNonQueryAsync(ct) > 0;
    }
}
