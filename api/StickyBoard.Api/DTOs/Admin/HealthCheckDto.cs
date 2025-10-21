namespace StickyBoard.Api.DTOs.Admin;

public record HealthCheckDto(string Status, string Version, TimeSpan Uptime);