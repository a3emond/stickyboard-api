using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.DTOs.Boards
{
    public sealed class BoardDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ParentBoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;

        // Structured JSON fields
        public Dictionary<string, object>? Theme { get; set; }
        public List<Dictionary<string, object>>? Rules { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateBoardDto
    {
        public string Title { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Guid? ParentBoardId { get; set; }
        public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;

        public Dictionary<string, object>? Theme { get; set; }
        public List<Dictionary<string, object>>? Rules { get; set; }
    }

    public sealed class UpdateBoardDto
    {
        public string? Title { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ParentBoardId { get; set; }
        public BoardVisibility? Visibility { get; set; }

        public Dictionary<string, object>? Theme { get; set; }
        public List<Dictionary<string, object>>? Rules { get; set; }
    }
}