namespace StickyBoard.Api.DTOs.Files
{
    // ------------------------------------------------------------
    // CREATE REQUEST
    // ------------------------------------------------------------
    public sealed class CreateFileDto
    {
        public Guid? BoardId { get; set; }
        public Guid? CardId { get; set; }

        // Required file storage information
        public string StorageKey { get; set; } = string.Empty; // unique storage identifier
        public string FileName { get; set; } = string.Empty;
        public string? MimeType { get; set; }

        public long SizeBytes { get; set; }

        // Optional structured metadata (JSON)
        public string? MetaJson { get; set; }
    }

    // ------------------------------------------------------------
    // READ / RESPONSE DTO
    // ------------------------------------------------------------
    public sealed class FileDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? BoardId { get; set; }
        public Guid? CardId { get; set; }

        public string StorageKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long SizeBytes { get; set; }

        public string? MetaJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ------------------------------------------------------------
    // LIGHTWEIGHT LIST ITEM (used in board view or file browser)
    // ------------------------------------------------------------
    public sealed class FileListItemDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long SizeBytes { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid? CardId { get; set; }
    }

    // ------------------------------------------------------------
    // DELETE RESPONSE
    // ------------------------------------------------------------
    public sealed class FileDeleteResponseDto
    {
        public bool Success { get; set; }
        public Guid? DeletedFileId { get; set; }
    }
}
