using Npgsql;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class OrganizationRepository : RepositoryBase<Organization>
    {
        public OrganizationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Organization Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Organization>(r);

        public override async Task<Guid> CreateAsync(Organization e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO organizations (name, owner_id)
                VALUES (@n, @o)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("n", e.Name);
            cmd.Parameters.AddWithValue("o", e.OwnerId);
            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Organization e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE organizations
                SET name=@n, updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("n", e.Name);
            cmd.Parameters.AddWithValue("id", e.Id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM organizations WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }
    }
}