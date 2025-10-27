using System;

namespace StickyBoard.Api.DTOs.Sections
{
    public sealed class SectionDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; }
        public string LayoutMeta { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; } = 0;
        public string? LayoutMeta { get; set; }
    }

    public sealed class UpdateSectionDto
    {
        public string? Title { get; set; }
        public int? Position { get; set; }
        public string? LayoutMeta { get; set; }
    }

    public sealed class ReorderSectionDto
    {
        public required Guid Id { get; set; }
        public required int Position { get; set; }
    }
}