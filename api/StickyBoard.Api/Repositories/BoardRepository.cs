using Npgsql;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class BoardRepository : RepositoryBase<Board>
    {
        public BoardRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Board Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Board>(r);

        // ------------------------------------------------------------
        // Core CRUD
        // ------------------------------------------------------------

        public override async Task<Guid> CreateAsync(Board e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO boards 
                    (owner_id, org_id, parent_board_id, title, visibility, theme, rules)
                VALUES 
                    (@owner, @org, @parent, @title, @vis, @theme, @rules)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("owner", e.OwnerId);
            cmd.Parameters.AddWithValue("org", (object?)e.OrganizationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("parent", (object?)e.ParentBoardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("vis", e.Visibility);
            cmd.Parameters.AddWithValue("theme", e.Theme);
            cmd.Parameters.AddWithValue("rules", e.Rules);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(Board e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE boards
                SET title=@title,
                    org_id=@org,
                    parent_board_id=@parent,
                    visibility=@vis,
                    theme=@theme,
                    rules=@rules,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("org", (object?)e.OrganizationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("parent", (object?)e.ParentBoardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("vis", e.Visibility);
            cmd.Parameters.AddWithValue("theme", e.Theme);
            cmd.Parameters.AddWithValue("rules", e.Rules);
            cmd.Parameters.AddWithValue("id", e.Id);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM boards WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // Custom Queries
        // ------------------------------------------------------------

        public async Task<IEnumerable<Board>> GetByOwnerAsync(Guid ownerId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE owner_id=@o ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("o", ownerId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> GetByOrgAsync(Guid orgId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE org_id=@org ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("org", orgId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> GetChildrenAsync(Guid parentId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE parent_board_id=@p ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("p", parentId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<Board?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT * FROM boards WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public async Task<IEnumerable<Board>> GetSharedBoardsAsync(CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE visibility!='private' ORDER BY created_at DESC", conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // Permission/Access Helper Queries
        // ------------------------------------------------------------

        public async Task<IEnumerable<Board>> GetAccessibleForUserAsync(Guid userId, CancellationToken ct)
        {
            // Boards owned by user or where user has permission
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT b.* 
                FROM boards b
                LEFT JOIN permissions p ON b.id=p.board_id
                WHERE b.owner_id=@u OR p.user_id=@u
                ORDER BY b.created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> SearchByTitleAsync(string keyword, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE LOWER(title) LIKE LOWER(@kw) ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("kw", $"%{keyword}%");
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // Maintenance
        // ------------------------------------------------------------

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT 1 FROM boards WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return (await cmd.ExecuteScalarAsync(ct)) != null;
        }
    }
}
