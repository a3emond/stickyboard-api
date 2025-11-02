using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards
{
    public class PermissionRepository : RepositoryBase<Permission>
    {
        public PermissionRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Permission Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Permission>(r);

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public override async Task<Guid> CreateAsync(Permission e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO permissions (user_id, board_id, role, granted_at)
                VALUES (@u, @b, @r, now())
                RETURNING board_id;", conn);

            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("b", e.BoardId);
            cmd.Parameters.AddWithValue("r", e.Role);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // ------------------------------------------------------------
        // UPDATE ROLE
        // ------------------------------------------------------------
        public override async Task<bool> UpdateAsync(Permission e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE permissions
                SET role = @r,
                    granted_at = now()
                WHERE user_id = @u AND board_id = @b;", conn);

            cmd.Parameters.AddWithValue("u", e.UserId);
            cmd.Parameters.AddWithValue("b", e.BoardId);
            cmd.Parameters.AddWithValue("r", e.Role);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // DELETE ALL PERMISSIONS ON A BOARD (boardId)
        // ------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid boardId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "DELETE FROM permissions WHERE board_id=@b;", conn);

            cmd.Parameters.AddWithValue("b", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // DELETE ONE USER ON ONE BOARD
        // ------------------------------------------------------------
        public async Task<bool> DeleteAsync(Guid boardId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM permissions
                WHERE board_id = @b AND user_id = @u;", conn);

            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("u", userId);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // GET ALL COLLABORATORS ON A BOARD
        // ------------------------------------------------------------
        public async Task<IEnumerable<Permission>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<Permission>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM permissions
                WHERE board_id = @b
                ORDER BY granted_at ASC;", conn);

            cmd.Parameters.AddWithValue("b", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // ------------------------------------------------------------
        // GET ALL BOARDS USER CAN ACCESS
        // ------------------------------------------------------------
        public async Task<IEnumerable<Permission>> GetByUserAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Permission>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM permissions
                WHERE user_id = @u
                ORDER BY granted_at DESC;", conn);

            cmd.Parameters.AddWithValue("u", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // ------------------------------------------------------------
        // GET SPECIFIC PERMISSION
        // ------------------------------------------------------------
        public async Task<Permission?> GetAsync(Guid boardId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM permissions
                WHERE board_id = @b AND user_id = @u
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("u", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        // ------------------------------------------------------------
        // CLEAR COLLABORATORS WHEN BOARD DELETED
        // ------------------------------------------------------------
        public async Task<int> DeleteByBoardAsync(Guid boardId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd =
                new NpgsqlCommand("DELETE FROM permissions WHERE board_id=@b;", conn);

            cmd.Parameters.AddWithValue("b", boardId);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
