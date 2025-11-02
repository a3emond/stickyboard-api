using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("tabs")]
    public class Tab : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("board_id")]
        public Guid BoardId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("tab_type")] public TabType TabType { get; set; } = TabType.board;

        [Column("layout_config")]
        public JsonDocument LayoutConfig { get; set; } = JsonDocument.Parse("{}");

        [Column("position")]
        public int Position { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}