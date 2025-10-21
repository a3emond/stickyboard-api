using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Boards
{
    [Table("boards")]
    public class Board : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("owner_id")] public Guid OwnerId { get; set; }
        [Column("title")] public string Title { get; set; } = string.Empty;
        [Column("visibility")] public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;
        [Column("theme")] public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");
        [Column("rules")] public JsonDocument Rules { get; set; } = JsonDocument.Parse("[]");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Table("permissions")]
    public class Permission
    {
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("role")] public BoardRole Role { get; set; }
        [Column("granted_at")] public DateTime GrantedAt { get; set; }
    }
}