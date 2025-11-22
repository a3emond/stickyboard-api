using Npgsql;
using NpgsqlTypes;
using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Attachments.Contracts;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments;

public sealed class AttachmentRepository
    : RepositoryBase<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(Attachment e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO attachments (
                workspace_id, board_id, card_id,
                filename, mime, byte_size,
                checksum_sha256, storage_path, is_public,
                status, meta, uploaded_by
            )
            VALUES (
                @ws, @b, @c,
                @fn, @mm, @bs,
                @cs, @sp, @pub,
                @st, @meta, @up
            )
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("ws", (object?)e.WorkspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("b", (object?)e.BoardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("c", (object?)e.CardId ?? DBNull.Value);

        cmd.Parameters.AddWithValue("fn", e.Filename);
        cmd.Parameters.AddWithValue("mm", (object?)e.Mime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bs", (object?)e.ByteSize ?? DBNull.Value);

        cmd.Parameters.AddWithValue("cs", (object?)e.ChecksumSha256 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sp", e.StoragePath);
        cmd.Parameters.AddWithValue("pub", e.IsPublic);

        cmd.Parameters.AddWithValue("st", e.Status);
        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        cmd.Parameters.AddWithValue("up", (object?)e.UploadedBy ?? DBNull.Value);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(Attachment e, CancellationToken ct)
    {
        var sql = $@"
            UPDATE attachments SET
                workspace_id = @ws,
                board_id     = @b,
                card_id      = @c,
                filename     = @fn,
                mime         = @mm,
                byte_size    = @bs,
                checksum_sha256 = @cs,
                storage_path = @sp,
                is_public    = @pub,
                status       = @st,
                meta         = @meta,
                uploaded_by  = @up
            WHERE {ConcurrencyWhere(e)};
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("ws", (object?)e.WorkspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("b", (object?)e.BoardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("c", (object?)e.CardId ?? DBNull.Value);

        cmd.Parameters.AddWithValue("fn", e.Filename);
        cmd.Parameters.AddWithValue("mm", (object?)e.Mime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bs", (object?)e.ByteSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cs", (object?)e.ChecksumSha256 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sp", e.StoragePath);
        cmd.Parameters.AddWithValue("pub", e.IsPublic);
        cmd.Parameters.AddWithValue("st", e.Status);

        cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb)
            .Value = e.Meta?.RootElement.GetRawText() ?? "{}";

        cmd.Parameters.AddWithValue("up", (object?)e.UploadedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", e.Id);
        BindConcurrencyParameters(cmd, e);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<Attachment>> GetForCardAsync(Guid cardId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
              FROM attachments
             WHERE card_id = @cid
             ORDER BY created_at ASC;
        ");

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", cardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<IEnumerable<Attachment>> GetForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
              FROM attachments
             WHERE board_id = @bid
             ORDER BY created_at ASC;
        ");

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bid", boardId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<IEnumerable<Attachment>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var sql = ApplySoftDeleteFilter(@"
            SELECT *
              FROM attachments
             WHERE workspace_id = @wid
             ORDER BY created_at DESC;
        ");

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("wid", workspaceId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}
