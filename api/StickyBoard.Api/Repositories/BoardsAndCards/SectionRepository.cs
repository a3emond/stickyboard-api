using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public class SectionRepository : RepositoryBase<Section>
{
    public SectionRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    protected override Section Map(NpgsqlDataReader reader)
        => MappingHelper.MapEntity<Section>(reader);

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public override async Task<Guid> CreateAsync(Section e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO sections (
                tab_id, parent_section_id, title, position, layout_meta
            )
            VALUES (
                @tab, @parent, @title, @pos, @meta
            )
            RETURNING id;", conn);

        cmd.Parameters.AddWithValue("tab", (object?)e.TabId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("parent", (object?)e.ParentSectionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("pos", e.Position);
        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.LayoutMeta?.RootElement.GetRawText() ?? "{}";

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    public override async Task<bool> UpdateAsync(Section e, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE sections
            SET title=@title,
                parent_section_id=@parent,
                position=@pos,
                layout_meta=@meta,
                updated_at=now()
            WHERE id=@id;", conn);

        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("title", e.Title);
        cmd.Parameters.AddWithValue("parent", (object?)e.ParentSectionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("pos", e.Position);
        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.LayoutMeta?.RootElement.GetRawText() ?? "{}";

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ------------------------------------------------------------
    // GET BY TAB
    // ------------------------------------------------------------
    public async Task<IEnumerable<Section>> GetByTabAsync(Guid tabId, CancellationToken ct)
    {
        var list = new List<Section>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM sections
            WHERE tab_id=@tab AND deleted_at IS NULL
            ORDER BY parent_section_id NULLS FIRST, position ASC;", conn);

        cmd.Parameters.AddWithValue("tab", tabId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // GET CHILDREN OF A SECTION (hierarchy)
    // ------------------------------------------------------------
    public async Task<IEnumerable<Section>> GetChildrenAsync(Guid parentId, CancellationToken ct)
    {
        var list = new List<Section>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM sections
            WHERE parent_section_id=@pid AND deleted_at IS NULL
            ORDER BY position ASC;", conn);

        cmd.Parameters.AddWithValue("pid", parentId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return list;
    }

    // ------------------------------------------------------------
    // REORDER WITHIN TAB OR PARENT
    // ------------------------------------------------------------
    public async Task<int> ReorderAsync(Guid tabId, IEnumerable<(Guid Id, int Pos)> positions, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var total = 0;
        foreach (var (id, pos) in positions)
        {
            await using var cmd = new NpgsqlCommand(@"
                UPDATE sections
                SET position=@pos, updated_at=now()
                WHERE id=@id AND tab_id=@tab;", conn, tx);

            cmd.Parameters.AddWithValue("tab", tabId);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("pos", pos);

            total += await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return total;
    }

    // ------------------------------------------------------------
    // DELETE ALL BY TAB
    // ------------------------------------------------------------
    public async Task<int> DeleteByTabAsync(Guid tabId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("DELETE FROM sections WHERE tab_id=@tab;", conn);
        cmd.Parameters.AddWithValue("tab", tabId);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> MoveAsync(Guid sectionId, Guid sectionTabId, Guid? dtoParentSectionId, int dtoNewPosition, CancellationToken ct)
    {
        // Load all sections under same tab
        var sections = await GetByTabAsync(sectionTabId, ct);

        if (!sections.Any(s => s.Id == sectionId))
            return false;

        var moving = sections.First(s => s.Id == sectionId);

        // Filter only siblings
        var siblings = sections
            .Where(s => s.ParentSectionId == dtoParentSectionId)
            .OrderBy(s => s.Position)
            .ToList();

        siblings.Remove(moving);

        dtoNewPosition = Math.Max(0, Math.Min(dtoNewPosition, siblings.Count));
        siblings.Insert(dtoNewPosition, moving);

        // Apply new order
        var updates = siblings
            .Select((s, idx) => (s.Id, Pos: idx))
            .ToList();

        // Commit
        await ReorderAsync(sectionTabId, updates, ct);

        // Update parent if changed
        if (moving.ParentSectionId != dtoParentSectionId)
        {
            moving.ParentSectionId = dtoParentSectionId;
            moving.Position = dtoNewPosition;
            await UpdateAsync(moving, ct);
        }

        return true;
    }


    public async Task<int> GetMaxPositionAsync(Guid tabId, Guid? parentSectionId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
        SELECT COALESCE(MAX(position), -1)
        FROM sections
        WHERE tab_id=@tab
        AND ((parent_section_id IS NULL AND @parent IS NULL)
             OR  parent_section_id=@parent)
        AND deleted_at IS NULL;", conn);

        cmd.Parameters.AddWithValue("tab", tabId);
        cmd.Parameters.AddWithValue("parent", (object?)parentSectionId ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

}
