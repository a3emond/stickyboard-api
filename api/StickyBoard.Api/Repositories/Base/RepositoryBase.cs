using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Npgsql;
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

        // Opens a mapped connection from the data source (supports cancellation)
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = await _dataSource.OpenConnectionAsync(ct);
            return conn;
        }

        protected string TableName =>
            typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name.ToLowerInvariant();

        protected abstract T Map(NpgsqlDataReader reader);

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

        public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);
        public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);
        public abstract Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
