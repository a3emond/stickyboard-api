using Npgsql;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories.Base;
using File = StickyBoard.Api.Models.FilesAndOps.File;

namespace StickyBoard.Api.Repositories
{
    public class FileRepository : RepositoryBase<File>
    {
        public FileRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override File Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<File>(reader);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public override async Task<Guid> CreateAsync(File e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO files (owner_id, board_id, card_id, storage_key, filename, mime_type, size_bytes, meta)
                VALUES (@owner, @board, @card, @key, @name, @mime, @size, @meta)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("owner", e.OwnerId);
            cmd.Parameters.AddWithValue("board", (object?)e.BoardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("card", (object?)e.CardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("key", e.StorageKey);
            cmd.Parameters.AddWithValue("name", e.FileName);
            cmd.Parameters.AddWithValue("mime", (object?)e.MimeType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", e.SizeBytes);
            cmd.Parameters.AddWithValue("meta", e.Meta.RootElement.GetRawText());

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        public override async Task<bool> UpdateAsync(File e, CancellationToken ct)
        {
            // File metadata is rarely updated, but allowed
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE files SET
                    mime_type=@mime,
                    size_bytes=@size,
                    meta=@meta,
                    updated_at=now()
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("mime", (object?)e.MimeType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", e.SizeBytes);
            cmd.Parameters.AddWithValue("meta", e.Meta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM files WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // ADDITIONAL QUERIES
        // ----------------------------------------------------------------------

        // Retrieve all files owned by a specific user
        public async Task<IEnumerable<File>> GetByOwnerAsync(Guid ownerId, CancellationToken ct)
        {
            var list = new List<File>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM files
                WHERE owner_id = @owner
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("owner", ownerId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all files attached to a specific board
        public async Task<IEnumerable<File>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<File>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM files
                WHERE board_id = @board
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve all files attached to a specific card
        public async Task<IEnumerable<File>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var list = new List<File>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM files
                WHERE card_id = @card
                ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("card", cardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }

        // Retrieve a file by its storage key (for validation or deduplication)
        public async Task<File?> GetByStorageKeyAsync(string key, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM files
                WHERE storage_key = @key
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("key", key);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
    }
}
