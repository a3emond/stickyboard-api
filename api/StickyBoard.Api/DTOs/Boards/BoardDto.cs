using StickyBoard.Api.Models.Enums;

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
        public string ThemeJson { get; set; } = "{}";
        public string RulesJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateBoardDto
    {
        public string Title { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Guid? ParentBoardId { get; set; }
        public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;
        public string? ThemeJson { get; set; }
        public string? RulesJson { get; set; }
    }

    public sealed class UpdateBoardDto
    {
        public string? Title { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ParentBoardId { get; set; }
        public BoardVisibility? Visibility { get; set; }
        public string? ThemeJson { get; set; }
        public string? RulesJson { get; set; }
    }
}