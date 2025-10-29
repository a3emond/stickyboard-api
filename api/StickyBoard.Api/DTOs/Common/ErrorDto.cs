namespace StickyBoard.Api.DTOs.Common
{
    public sealed class ErrorDto
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}