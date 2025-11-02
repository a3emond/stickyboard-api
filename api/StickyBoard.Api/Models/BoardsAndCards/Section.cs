using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("sections")]
    public class Section : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("tab_id")]
        public Guid TabId { get; set; }
        
        [Column("parent_section_id")]
        public Guid? ParentSectionId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("position")]
        public int Position { get; set; }

        [Column("layout_meta")]
        public JsonDocument LayoutMeta { get; set; } = JsonDocument.Parse("{}");

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }

}