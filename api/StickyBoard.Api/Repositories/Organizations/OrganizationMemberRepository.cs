using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Organizations
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
                INSERT INTO organization_members (org_id, user_id, role, joined_at)
                VALUES (@org, @user, @role, NOW())
                RETURNING org_id", conn);

            cmd.Parameters.AddWithValue("org", e.OrgId);
            cmd.Parameters.AddWithValue("user", e.UserId);
            cmd.Parameters.AddWithValue("role", e.Role);

            await cmd.ExecuteScalarAsync(ct);
            return e.OrgId;
        }

        public override async Task<bool> UpdateAsync(OrganizationMember e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE organization_members
                SET role = @role
                WHERE org_id = @org AND user_id = @user", conn);

            cmd.Parameters.AddWithValue("role", e.Role);
            cmd.Parameters.AddWithValue("org", e.OrgId);
            cmd.Parameters.AddWithValue("user", e.UserId);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // Hard delete is correct for membership table
        public override async Task<bool> DeleteAsync(Guid orgId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd =
                new NpgsqlCommand("DELETE FROM organization_members WHERE org_id = @org", conn);

            cmd.Parameters.AddWithValue("org", orgId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> RemoveMemberAsync(Guid orgId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd =
                new NpgsqlCommand("DELETE FROM organization_members WHERE org_id = @org AND user_id = @user", conn);

            cmd.Parameters.AddWithValue("org", orgId);
            cmd.Parameters.AddWithValue("user", userId);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<IEnumerable<OrganizationMember>> GetByOrganizationAsync(Guid orgId, CancellationToken ct)
        {
            var list = new List<OrganizationMember>();
            await using var conn = await OpenAsync(ct);
            await using var cmd =
                new NpgsqlCommand("SELECT * FROM organization_members WHERE org_id = @org", conn);

            cmd.Parameters.AddWithValue("org", orgId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
        
        public async Task<OrganizationMember?> GetMemberAsync(Guid orgId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
        SELECT * FROM organization_members
        WHERE org_id = @org AND user_id = @user
        LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("org", orgId);
            cmd.Parameters.AddWithValue("user", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public async Task<bool> ExistsAsync(Guid orgId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
        SELECT 1 FROM organization_members
        WHERE org_id = @org AND user_id = @user;", conn);

            cmd.Parameters.AddWithValue("org", orgId);
            cmd.Parameters.AddWithValue("user", userId);

            return await cmd.ExecuteScalarAsync(ct) is not null;
        }

    }
}
