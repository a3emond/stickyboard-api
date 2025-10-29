using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Npgsql;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Repositories.Base; // for MappingHelper

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
        // CONNECTION
        // ------------------------------------------------------------
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = await _dataSource.OpenConnectionAsync(ct);
            return conn;
        }

        protected string TableName =>
            typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name.ToLowerInvariant();

        // ------------------------------------------------------------
        // MAPPING
        // ------------------------------------------------------------

        // Legacy pattern support: still requires each repo to implement Map(reader)
        protected abstract T Map(NpgsqlDataReader reader);

        // Optional generic fallback using MappingHelper
        protected T MapEntity(NpgsqlDataReader reader) => MappingHelper.MapEntity<T>(reader);

        // Shared helper for list mapping
        protected async Task<List<T>> MapReaderToListAsync(NpgsqlDataReader reader, CancellationToken ct)
        {
            var list = new List<T>();
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader)); // still calls the repository-specific Map() override
            return list;
        }

        // Synchronous variant if you ever need it (used below)
        protected List<T> MapReaderToList(NpgsqlDataReader reader)
        {
            var list = new List<T>();
            while (reader.Read())
                list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // COMMON CRUD
        // ------------------------------------------------------------
        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand($"SELECT * FROM {TableName} WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
        {
            var list = new List<T>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand($"SELECT * FROM {TableName}", conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // ------------------------------------------------------------
        // SYNC SUPPORT (delta fetch)
        // ------------------------------------------------------------
        public virtual async Task<IEnumerable<T>> GetUpdatedSinceAsync(DateTime since, CancellationToken ct)
        {
            var sql = $"SELECT * FROM {TableName} WHERE updated_at > @since";

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("since", since);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return MapReaderToList(reader); // uses synchronous variant for convenience
        }

        // ------------------------------------------------------------
        // ABSTRACT WRITES
        // ------------------------------------------------------------
        public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);
        public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);
        public abstract Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
