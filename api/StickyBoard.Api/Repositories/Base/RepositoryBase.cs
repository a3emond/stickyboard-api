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

        // Opens a mapped connection from the data source
        protected async Task<NpgsqlConnection> OpenAsync()
        {
            var conn = await _dataSource.OpenConnectionAsync();
            return conn;
        }

        protected string TableName =>
            typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name.ToLowerInvariant();

        protected abstract T Map(NpgsqlDataReader reader);

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new NpgsqlCommand($"SELECT * FROM {TableName} WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            var list = new List<T>();
            await using var conn = await OpenAsync();
            await using var cmd = new NpgsqlCommand($"SELECT * FROM {TableName}", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public abstract Task<Guid> CreateAsync(T entity);
        public abstract Task<bool> UpdateAsync(T entity);
        public abstract Task<bool> DeleteAsync(Guid id);
    }
}