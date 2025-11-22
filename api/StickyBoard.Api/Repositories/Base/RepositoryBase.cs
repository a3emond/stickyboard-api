using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Npgsql;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Repositories.Base
{
    public abstract class RepositoryBase<T> : IRepository<T>, ISyncRepository<T>
        where T : class, IEntity, new()
    {
        private readonly NpgsqlDataSource _db;

        protected RepositoryBase(NpgsqlDataSource db) => _db = db;

        protected string Table =>
            typeof(T).GetCustomAttribute<TableAttribute>()?.Name
            ?? typeof(T).Name.ToLowerInvariant();

        protected ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
            => _db.OpenConnectionAsync(ct);

        // ---------------------------------------------------------------------
        // Soft Delete Helpers
        // ---------------------------------------------------------------------
        private bool SoftEnabled => typeof(ISoftDeletable).IsAssignableFrom(typeof(T));
        private bool IncludeDeleted => this is IAllowDeleted allow && allow.IncludeDeleted;

        protected string ApplySoftDeleteFilter(string sql)
        {
            if (!SoftEnabled || IncludeDeleted)
                return sql;

            return sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
                ? sql + " AND deleted_at IS NULL"
                : sql + " WHERE deleted_at IS NULL";
        }

        // ---------------------------------------------------------------------
        // Mapping
        // ---------------------------------------------------------------------
        protected virtual T MapRow(NpgsqlDataReader r)
            => MappingHelper.MapEntity<T>(r);

        protected async Task<List<T>> MapListAsync(NpgsqlDataReader r, CancellationToken ct)
        {
            var list = new List<T>();
            while (await r.ReadAsync(ct))
                list.Add(MapRow(r));

            return list;
        }

        // ---------------------------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------------------------
        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($"SELECT * FROM {Table} WHERE id = @id");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("id", id);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? MapRow(r) : null;
        }

        public async Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct)
        {
            var sql = $"SELECT * FROM {Table} WHERE id = @id";

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("id", id);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? MapRow(r) : null;
        }

        // ---------------------------------------------------------------------
        // GET ALL
        // ---------------------------------------------------------------------
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($"SELECT * FROM {Table}");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            await using var r = await cmd.ExecuteReaderAsync(ct);

            return await MapListAsync(r, ct);
        }

        // ---------------------------------------------------------------------
        // EXISTS
        // ---------------------------------------------------------------------
        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($"SELECT 1 FROM {Table} WHERE id = @id");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("id", id);

            return await cmd.ExecuteScalarAsync(ct) is not null;
        }

        // ---------------------------------------------------------------------
        // COUNT
        // ---------------------------------------------------------------------
        public async Task<int> CountAsync(CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($"SELECT COUNT(*) FROM {Table}");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        // ---------------------------------------------------------------------
        // PAGING (single-query with window function)
        // ---------------------------------------------------------------------
        public async Task<PagedResult<T>> GetPagedAsync(int limit, int offset, CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($@"
                SELECT *, COUNT(*) OVER() AS total_count
                FROM {Table}
                ORDER BY updated_at DESC
                LIMIT @limit OFFSET @offset");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("limit", limit);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var r = await cmd.ExecuteReaderAsync(ct);

            var items = new List<T>();
            int total = 0;

            while (await r.ReadAsync(ct))
            {
                items.Add(MapRow(r));
                total = r.GetInt32(r.GetOrdinal("total_count"));
            }

            return PagedResult<T>.Create(items, total, limit, offset);
        }

        // ---------------------------------------------------------------------
        // SYNC (delta + single-query paging)
        // ---------------------------------------------------------------------
        public async Task<IEnumerable<T>> GetUpdatedSinceAsync(DateTime since, CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($@"
                SELECT * FROM {Table}
                WHERE updated_at > @since");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("since", since);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await MapListAsync(r, ct);
        }

        public async Task<PagedResult<T>> GetUpdatedSincePagedAsync(
            DateTime since, int limit, int offset, CancellationToken ct)
        {
            var sql = ApplySoftDeleteFilter($@"
                SELECT *, COUNT(*) OVER() AS total_count
                FROM {Table}
                WHERE updated_at > @since
                ORDER BY updated_at DESC
                LIMIT @limit OFFSET @offset");

            await using var c = await Conn(ct);
            await using var cmd = new NpgsqlCommand(sql, c);
            cmd.Parameters.AddWithValue("since", since);
            cmd.Parameters.AddWithValue("limit", limit);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var r = await cmd.ExecuteReaderAsync(ct);

            var items = new List<T>();
            int total = 0;

            while (await r.ReadAsync(ct))
            {
                items.Add(MapRow(r));
                total = r.GetInt32(r.GetOrdinal("total_count"));
            }

            return PagedResult<T>.Create(items, total, limit, offset);
        }

        // ---------------------------------------------------------------------
        // CREATE (implemented by subclasses)
        // ---------------------------------------------------------------------
        public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);

        // ---------------------------------------------------------------------
        // UPDATE (must enforce concurrency if entity is versioned)
        // ---------------------------------------------------------------------
        public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);

        protected string ConcurrencyWhere(T entity)
        {
            if (entity is IVersionedEntity v)
                return "id = @id AND version = @version";
            return "id = @id";
        }

        protected void BindConcurrencyParameters(NpgsqlCommand cmd, T entity)
        {
            if (entity is IVersionedEntity v)
                cmd.Parameters.AddWithValue("version", v.Version);
        }

        // ---------------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------------
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var c = await Conn(ct);

            if (SoftEnabled)
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
}
