using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("boards")]
    public class Board : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [Column("org_id")]
        public Guid? OrgId { get; set; }
        
        [Column("folder_id")]
        public Guid? FolderId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("visibility")]
        public BoardVisibility Visibility { get; set; } = BoardVisibility.private_;

        [Column("theme")]
        public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");
        
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