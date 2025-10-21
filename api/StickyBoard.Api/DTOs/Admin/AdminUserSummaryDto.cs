namespace StickyBoard.Api.DTOs.Admin;

public record AdminUserSummaryDto(Guid Id, string Email, string Role, DateTime CreatedAt);