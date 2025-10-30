using System;

namespace StickyBoard.Api.DTOs.Files
{
    public sealed class CreateFileDto
    {
        public Guid? BoardId { get; set; }
        public Guid? CardId { get; set; }

        public string StorageKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long SizeBytes { get; set; }

        public Dictionary<string, object>? Meta { get; set; }
    }

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

        public Dictionary<string, object>? Meta { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class FileListItemDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CardId { get; set; }
    }

    public sealed class FileDeleteResponseDto
    {
        public bool Success { get; set; }
        public Guid? DeletedFileId { get; set; }
    }
}