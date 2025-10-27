using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Tabs
{
    public sealed class TabDto
    {
        public Guid Id { get; set; }
        public TabScope Scope { get; set; }
        public Guid BoardId { get; set; }
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TabType { get; set; } = "custom";
        public string LayoutConfig { get; set; } = "{}";
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateTabDto
    {
        public TabScope Scope { get; set; } = TabScope.board;
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TabType { get; set; } = "custom";
        public string? LayoutConfig { get; set; }
        public int Position { get; set; } = 0;
    }

    public sealed class UpdateTabDto
    {
        public string? Title { get; set; }
        public string? TabType { get; set; }
        public string? LayoutConfig { get; set; }
        public int? Position { get; set; }
    }

    public sealed class ReorderTabDto
    {
        public required Guid Id { get; set; }
        public required int Position { get; set; }
    }
}