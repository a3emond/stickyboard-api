using Npgsql;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class TagRepository : RepositoryBase<Tag>
    {
        public TagRepository(string connectionString) : base(connectionString) { }

        protected override Tag Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Tag>(reader);

        public override async Task<Guid> CreateAsync(Tag e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO tags (name) VALUES (@name)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("name", e.Name);
            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Tag e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE tags SET name=@name WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("name", e.Name);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM tags WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}