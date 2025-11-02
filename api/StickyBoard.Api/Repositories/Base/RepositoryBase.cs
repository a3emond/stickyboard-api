using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base
{
    public abstract class RepositoryBase<T> : IRepository<T> where T : class, IEntity, new()
    {
        private readonly NpgsqlDataSource _dataSource;

        protected RepositoryBase(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        // ------------------------------------------------------------
        // Connection
        // ------------------------------------------------------------
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            return await _dataSource.OpenConnectionAsync(ct);
        }

        protected string TableName =>
            typeof(T).GetCustomAttribute<TableAttribute>()?.Name
            ?? typeof(T).Name.ToLowerInvariant();

        // ------------------------------------------------------------
        // Mapping
        // ------------------------------------------------------------
        protected abstract T Map(NpgsqlDataReader reader);

        protected T MapEntity(NpgsqlDataReader reader) =>
            MappingHelper.MapEntity<T>(reader);

        protected async Task<List<T>> MapReaderToListAsync(NpgsqlDataReader reader, CancellationToken ct)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        protected List<T> MapReaderToList(NpgsqlDataReader reader)
        {
            var list = new List<T>();
            while (reader.Read())
                list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // Read
        // ------------------------------------------------------------
        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var sql = $"SELECT * FROM {TableName} WHERE id = @id";

            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                sql += " AND deleted_at IS NULL";

            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
        {
            var sql = $"SELECT * FROM {TableName}";

            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                sql += " WHERE deleted_at IS NULL";

            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            return await MapReaderToListAsync(reader, ct);
        }

        // ------------------------------------------------------------
        // Sync / Delta Query
        // ------------------------------------------------------------
        public virtual async Task<IEnumerable<T>> GetUpdatedSinceAsync(DateTime since, CancellationToken ct)
        {
            var sql = $"SELECT * FROM {TableName} WHERE updated_at > @since";

            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                sql += " AND deleted_at IS NULL";

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("since", since);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return MapReaderToList(reader);
        }

        // ------------------------------------------------------------
        // Write (abstract)
        // ------------------------------------------------------------
        public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);
        public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);

        // ------------------------------------------------------------
        // Delete — Soft Delete if supported
        // ------------------------------------------------------------
        public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);

            // Soft delete if supported
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                var sql = $"UPDATE {TableName} SET deleted_at = NOW() WHERE id = @id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", id);

                return await cmd.ExecuteNonQueryAsync(ct) > 0;
            }

            // Hard delete fallback
            var hardSql = $"DELETE FROM {TableName} WHERE id = @id";
            await using var hardCmd = new NpgsqlCommand(hardSql, conn);
            hardCmd.Parameters.AddWithValue("id", id);

            return await hardCmd.ExecuteNonQueryAsync(ct) > 0;
        }
    }
}
