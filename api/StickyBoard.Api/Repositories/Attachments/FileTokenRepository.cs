using Npgsql;
using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Attachments.Contracts;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.Attachments;

public sealed class FileTokenRepository
    : RepositoryBase<FileToken>, IFileTokenRepository
{
    public FileTokenRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

    public override async Task<Guid> CreateAsync(FileToken e, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO file_tokens (
                attachment_id, variant, secret,
                audience, expires_at, created_by, revoked
            )
            VALUES (
                @aid, @var, @sec,
                @aud, @exp, @cb, @rv
            )
            RETURNING id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("aid", e.AttachmentId);
        cmd.Parameters.AddWithValue("var", (object?)e.Variant ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sec", e.Secret);
        cmd.Parameters.AddWithValue("aud", e.Audience);
        cmd.Parameters.AddWithValue("exp", e.ExpiresAt);
        cmd.Parameters.AddWithValue("cb", (object?)e.CreatedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("rv", e.Revoked);

        return (Guid)await cmd.ExecuteScalarAsync(ct);
    }

    public override async Task<bool> UpdateAsync(FileToken e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE file_tokens SET
                revoked = @rv
             WHERE id = @id;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("rv", e.Revoked);
        cmd.Parameters.AddWithValue("id", e.Id);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IEnumerable<FileToken>> GetValidForAttachmentAsync(Guid attachmentId, DateTime now, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM file_tokens
             WHERE attachment_id = @aid
               AND revoked       = FALSE
               AND expires_at   > @now
             ORDER BY created_at DESC;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("aid", attachmentId);
        cmd.Parameters.AddWithValue("now", now);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    public async Task<bool> RevokeAsync(Guid tokenId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE file_tokens
               SET revoked = TRUE
             WHERE id = @id
               AND revoked = FALSE;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", tokenId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<int> RevokeAllForAttachmentAsync(Guid attachmentId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE file_tokens
               SET revoked = TRUE
             WHERE attachment_id = @aid
               AND revoked = FALSE;
        ";

        await using var conn = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("aid", attachmentId);

        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
