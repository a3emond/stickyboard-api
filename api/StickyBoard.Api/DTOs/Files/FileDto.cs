namespace StickyBoard.Api.DTOs.Files;

public record FileDto(
    Guid Id,
    Guid OwnerId,
    Guid? BoardId,
    Guid? CardId,
    string Filename,
    string? MimeType,
    long? SizeBytes,
    object Meta,
    DateTime CreatedAt,
    string? Url
);