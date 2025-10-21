namespace StickyBoard.Api.DTOs.Admin;

public record AdminBoardSummaryDto(Guid Id, string Title, string Visibility, DateTime CreatedAt);