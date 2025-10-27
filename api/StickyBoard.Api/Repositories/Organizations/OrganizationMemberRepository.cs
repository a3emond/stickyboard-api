using Npgsql;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class OrganizationMemberRepository : RepositoryBase<OrganizationMember>
    {
        public OrganizationMemberRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override OrganizationMember Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<OrganizationMember>(r);

        public override async Task<Guid> CreateAsync(OrganizationMember e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO organization_members (org_id, user_id, role)
                VALUES (@o, @u, @r)
                RETURNING org_id", conn);

            cmd.Parameters.AddWithValue("o", e.OrganizationId);
            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("r", e.Role);
            await cmd.ExecuteScalarAsync(ct);
            return e.OrganizationId;
        }

        public override async Task<bool> UpdateAsync(OrganizationMember e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE organization_members
                SET role=@r
                WHERE org_id=@o AND user_id=@u", conn);

            cmd.Parameters.AddWithValue("r", e.Role);
            cmd.Parameters.AddWithValue("o", e.OrganizationId);
            cmd.Parameters.AddWithValue("u", e.UserId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            // Removes all members from org (used on org deletion)
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM organization_members WHERE org_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> RemoveMemberAsync(Guid orgId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM organization_members WHERE org_id=@o AND user_id=@u", conn);
            cmd.Parameters.AddWithValue("o", orgId);
            cmd.Parameters.AddWithValue("u", userId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid orgId, CancellationToken ct)
        {
            var list = new List<OrganizationMember>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM organization_members WHERE org_id=@o", conn);
            cmd.Parameters.AddWithValue("o", orgId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }
    }
}
