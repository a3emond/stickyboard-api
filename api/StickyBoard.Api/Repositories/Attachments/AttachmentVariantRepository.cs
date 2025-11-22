using Npgsql;
using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Attachments.Contracts;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments;

public sealed class AttachmentVariantRepository
    : RepositoryBase<AttachmentVariant>, IAttachmentVariantRepository
{
    public AttachmentVariantRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(AttachmentVariant e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO attachment_variants (
                parent_id, variant, mime,
                byte_size, width, height,
                duration_ms, storage_path, status,
                checksum_sha256
            )
            VALUES (
                @pid, @var, @mm,
                @bs, @w, @h,
                @dur, @sp, @st,
                @cs
            )
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("pid", e.ParentId);
        cmd.Parameters.AddWithValue("var", e.Variant);
        cmd.Parameters.AddWithValue("mm", e.Mime);
        cmd.Parameters.AddWithValue("bs", (object?)e.ByteSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("w", (object?)e.Width ?? DBNull.Value);
        cmd.Parameters.AddWithValue("h", (object?)e.Height ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dur", (object?)e.DurationMs ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sp", e.StoragePath);
        cmd.Parameters.AddWithValue("st", e.Status);
        cmd.Parameters.AddWithValue("cs", (object?)e.ChecksumSha256 ?? DBNull.Value);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(AttachmentVariant e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE attachment_variants SET
                mime         = @mm,
                byte_size    = @bs,
                width        = @w,
                height       = @h,
                duration_ms  = @dur,
                storage_path = @sp,
                status       = @st,
                checksum_sha256 = @cs
             WHERE id = @id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("mm", e.Mime);
        cmd.Parameters.AddWithValue("bs", (object?)e.ByteSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("w", (object?)e.Width ?? DBNull.Value);
        cmd.Parameters.AddWithValue("h", (object?)e.Height ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dur", (object?)e.DurationMs ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sp", e.StoragePath);
        cmd.Parameters.AddWithValue("st", e.Status);
        cmd.Parameters.AddWithValue("cs", (object?)e.ChecksumSha256 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", e.Id);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<AttachmentVariant>> GetForParentAsync(Guid parentId, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM attachment_variants
             WHERE parent_id = @pid
             ORDER BY created_at ASC;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("pid", parentId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }
}
