using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Organizations;

public class OrganizationRepository : RepositoryBase<Organization>
{
    public OrganizationRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override Organization Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<Organization>(r);

    public override async Task<Guid> CreateAsync(Organization e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO organizations (name, owner_id, created_at, updated_at)
            VALUES (@name, @owner, NOW(), NOW())
            RETURNING id", conn);

        cmd.Parameters.AddWithValue("name", e.Name);
        cmd.Parameters.AddWithValue("owner", e.OwnerId);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Organization e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE organizations
            SET name = @name,
                updated_at = NOW()
            WHERE id = @id 
              AND deleted_at IS NULL", conn);

        cmd.Parameters.AddWithValue("name", e.Name);
        cmd.Parameters.AddWithValue("id", e.Id);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // DO NOT override DeleteAsync
    // Soft delete happens automatically in RepositoryBase
    // because Organization implements ISoftDeletable

    public async Task<IEnumerable<Organization>> GetByOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        var list = new List<Organization>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM organizations
            WHERE owner_id = @owner
              AND deleted_at IS NULL
            ORDER BY created_at DESC", conn);

        cmd.Parameters.AddWithValue("owner", ownerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }
    
    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
        SELECT * FROM organizations
        WHERE id = @id AND deleted_at IS NULL
        LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IEnumerable<Organization>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = new List<Organization>();

        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
        SELECT o.* 
        FROM organizations o
        JOIN organization_members m ON m.org_id = o.id
        WHERE m.user_id = @uid
          AND o.deleted_at IS NULL
        ORDER BY o.created_at DESC;", conn);

        cmd.Parameters.AddWithValue("uid", userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

}
