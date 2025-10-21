using Npgsql;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories.Base;
using File = StickyBoard.Api.Models.FilesAndOps.File;

namespace StickyBoard.Api.Repositories
{
    public class FileRepository : RepositoryBase<File>
    {
        public FileRepository(string connectionString) : base(connectionString) { }

        protected override File Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<File>(reader);

        public override async Task<Guid> CreateAsync(File e)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
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

            return (Guid)await cmd.ExecuteScalarAsync();
        }

        public override async Task<bool> UpdateAsync(File e)
        {
            // File metadata is rarely updated, but allowed
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                UPDATE files SET
                    mime_type=@mime,
                    size_bytes=@size,
                    meta=@meta
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("mime", (object?)e.MimeType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", e.SizeBytes);
            cmd.Parameters.AddWithValue("meta", e.Meta.RootElement.GetRawText());

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM files WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
