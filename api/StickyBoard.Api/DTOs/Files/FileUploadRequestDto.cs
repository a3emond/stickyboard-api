namespace StickyBoard.Api.DTOs.Files;

public record FileUploadRequestDto(
    string Filename,
    string? MimeType,
    Guid? BoardId,
    Guid? CardId
);