namespace StickyBoard.Api.DTOs.Auth;

public record RegisterRequestDto(string Email, string Password, string DisplayName);