using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;
using System.Text.Json;

namespace StickyBoard.Api.Repositories.BoardsAndCards
{
    public class BoardRepository : RepositoryBase<Board>
    {
        public BoardRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override Board Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<Board>(r);

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public override async Task<Guid> CreateAsync(Board e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO boards 
                    (owner_id, org_id, folder_id, title, visibility, theme, meta)
                VALUES 
                    (@owner, @org, @folder, @title, @vis, @theme, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("owner", e.OwnerId);
            cmd.Parameters.AddWithValue("org", (object?)e.OrgId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("folder", (object?)e.FolderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("vis", e.Visibility);

            cmd.Parameters.Add("theme", NpgsqlDbType.Jsonb)
                .Value = e.Theme?.RootElement.GetRawText() ?? "{}";

            cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
                .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // ------------------------------------------------------------
        // UPDATE (full entity)
        // ------------------------------------------------------------
        public override async Task<bool> UpdateAsync(Board e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE boards
                SET title=@title,
                    org_id=@org,
                    folder_id=@folder,
                    visibility=@vis,
                    theme=@theme,
                    meta=@meta,
                    updated_at=now()
                WHERE id=@id AND deleted_at IS NULL", conn);

            cmd.Parameters.AddWithValue("title", e.Title);
            cmd.Parameters.AddWithValue("org", (object?)e.OrgId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("folder", (object?)e.FolderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("vis", e.Visibility);

            cmd.Parameters.Add("theme", NpgsqlDbType.Jsonb)
                .Value = e.Theme?.RootElement.GetRawText() ?? "{}";

            cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
                .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

            cmd.Parameters.AddWithValue("id", e.Id);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // PARTIAL UPDATES (quality-of-life helpers)
        // ------------------------------------------------------------
        public async Task<bool> RenameAsync(Guid boardId, string title, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE boards SET title=@t, updated_at=now() WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("t", title);
            cmd.Parameters.AddWithValue("id", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> MoveToFolderAsync(Guid boardId, Guid? folderId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE boards SET folder_id=@f, updated_at=now() WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("f", (object?)folderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> MoveToOrgAsync(Guid boardId, Guid? orgId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE boards SET org_id=@o, updated_at=now() WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("o", (object?)orgId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> UpdateThemeAsync(Guid boardId, JsonDocument theme, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE boards SET theme=@theme, updated_at=now() WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.Add("theme", NpgsqlDbType.Jsonb).Value = theme?.RootElement.GetRawText() ?? "{}";
            cmd.Parameters.AddWithValue("id", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<bool> UpdateMetaAsync(Guid boardId, JsonDocument meta, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE boards SET meta=@meta, updated_at=now() WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb).Value = meta?.RootElement.GetRawText() ?? "{}";
            cmd.Parameters.AddWithValue("id", boardId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // SOFT DELETE handled by base — DO NOT OVERRIDE
        // ------------------------------------------------------------

        // ------------------------------------------------------------
        // BASICS / LOAD BY ID(S)
        // ------------------------------------------------------------
        public async Task<Board?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE id=@id AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? Map(r) : null;
        }

        public async Task<IEnumerable<Board>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
        {
            var arr = ids?.Distinct().ToArray() ?? Array.Empty<Guid>();
            if (arr.Length == 0) return Array.Empty<Board>();

            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE id = ANY(@ids) AND deleted_at IS NULL", conn);
            cmd.Parameters.Add("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).Value = arr;
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        // ------------------------------------------------------------
        // OWNER / ORG / FOLDER SCOPED
        // ------------------------------------------------------------
        public async Task<IEnumerable<Board>> GetByOwnerAsync(Guid ownerId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE owner_id=@o AND deleted_at IS NULL ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("o", ownerId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> GetByOrgAsync(Guid orgId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE org_id=@org AND deleted_at IS NULL ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("org", orgId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> GetByFolderAsync(Guid folderId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE folder_id=@folder AND deleted_at IS NULL ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("folder", folderId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) list.Add(Map(reader));
            return list;
        }

        // ------------------------------------------------------------
        // PUBLIC / MEMBERSHIP / VISIBILITY
        // ------------------------------------------------------------
        public async Task<IEnumerable<Board>> GetPublicBoardsAsync(CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE visibility='public_' AND deleted_at IS NULL ORDER BY created_at DESC", conn);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        public async Task<IEnumerable<Board>> GetByMembershipAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT b.*
                FROM boards b
                JOIN permissions p ON p.board_id = b.id
                WHERE p.user_id=@u AND b.deleted_at IS NULL
                ORDER BY b.created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        /// <summary>
        /// Boards the user can see: owned OR membership OR public OR org-shared where user is org member and board.visibility in ('shared','public_').
        /// </summary>
        public async Task<IEnumerable<Board>> GetVisibleToUserAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT b.*
                FROM boards b
                LEFT JOIN permissions p ON p.board_id = b.id AND p.user_id=@u
                LEFT JOIN organizations o ON o.id = b.org_id
                LEFT JOIN organization_members om ON om.org_id = b.org_id AND om.user_id=@u
                WHERE b.deleted_at IS NULL
                  AND (
                        b.owner_id=@u
                     OR p.user_id IS NOT NULL
                     OR b.visibility='public_'
                     OR (b.org_id IS NOT NULL AND om.user_id IS NOT NULL AND b.visibility IN ('shared','public_'))
                  )
                ORDER BY b.created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        public async Task<IEnumerable<Board>> GetVisibleToUserInOrgAsync(Guid userId, Guid orgId, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT b.*
                FROM boards b
                LEFT JOIN permissions p ON p.board_id = b.id AND p.user_id=@u
                LEFT JOIN organization_members om ON om.org_id = b.org_id AND om.user_id=@u
                WHERE b.deleted_at IS NULL AND b.org_id=@org
                  AND (
                        b.owner_id=@u
                     OR p.user_id IS NOT NULL
                     OR b.visibility='public_'
                     OR (om.user_id IS NOT NULL AND b.visibility IN ('shared','public_'))
                  )
                ORDER BY b.created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("org", orgId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        // ------------------------------------------------------------
        // SEARCH
        // ------------------------------------------------------------
        public async Task<IEnumerable<Board>> SearchByTitleAsync(string keyword, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM boards WHERE deleted_at IS NULL AND LOWER(title) LIKE LOWER(@kw) ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("kw", $"%{keyword}%");
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) list.Add(Map(reader));
            return list;
        }

        public async Task<IEnumerable<Board>> SearchVisibleByTitleAsync(Guid userId, string keyword, CancellationToken ct)
        {
            var list = new List<Board>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT b.*
                FROM boards b
                LEFT JOIN permissions p ON p.board_id = b.id AND p.user_id=@u
                LEFT JOIN organization_members om ON om.org_id = b.org_id AND om.user_id=@u
                WHERE b.deleted_at IS NULL
                  AND LOWER(b.title) LIKE LOWER(@kw)
                  AND (
                        b.owner_id=@u
                     OR p.user_id IS NOT NULL
                     OR b.visibility='public_'
                     OR (b.org_id IS NOT NULL AND om.user_id IS NOT NULL AND b.visibility IN ('shared','public_'))
                  )
                ORDER BY b.created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("kw", $"%{keyword}%");
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(Map(r));
            return list;
        }

        // ------------------------------------------------------------
        // COUNTS (dashboard / quotas)
        // ------------------------------------------------------------
        public async Task<int> CountForUserAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(1) FROM boards WHERE owner_id=@u AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("u", userId);
            var o = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(o);
        }

        public async Task<int> CountForOrgAsync(Guid orgId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(1) FROM boards WHERE org_id=@o AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("o", orgId);
            var o = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(o);
        }

        // ------------------------------------------------------------
        // PERMISSIONS HELPERS (read-only projections)
        // ------------------------------------------------------------
        public async Task<(bool exists, string? role)> TryGetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT role::text
                FROM permissions
                WHERE board_id=@b AND user_id=@u", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("u", userId);
            var res = await cmd.ExecuteScalarAsync(ct) as string;
            return (res != null, res);
        }

        public async Task<bool> IsOwnerAsync(Guid boardId, Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM boards WHERE id=@b AND owner_id=@u AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            cmd.Parameters.AddWithValue("u", userId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct);
        }

        public async Task<bool> ExistsAsync(Guid boardId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM boards WHERE id=@b AND deleted_at IS NULL", conn);
            cmd.Parameters.AddWithValue("b", boardId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct);
        }
    }
}
