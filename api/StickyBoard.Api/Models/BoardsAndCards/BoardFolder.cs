using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("board_folders")]
    public class BoardFolder : IEntityUpdatable, ISoftDeletable
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("org_id")]
        public Guid? OrgId { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("icon")]
        public string? Icon { get; set; }

        [Column("color")]
        public string? Color { get; set; }

        [Column("meta")]
        public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}