using Npgsql;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class LinkRepository : RepositoryBase<Link>
    {
        public LinkRepository(string connectionString) : base(connectionString) { }

        protected override Link Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<Link>(reader);

        public override async Task<Guid> CreateAsync(Link e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO links (from_card, to_card, rel_type, created_by)
                VALUES (@from, @to, @rel, @creator)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("from", e.FromCard);
            cmd.Parameters.AddWithValue("to", e.ToCard);
            cmd.Parameters.AddWithValue("rel", e.RelType.ToString());
            cmd.Parameters.AddWithValue("creator", (object?)e.CreatedBy ?? DBNull.Value);

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(Link e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE links SET rel_type=@rel WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("rel", e.RelType.ToString());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM links WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}