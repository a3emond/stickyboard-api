using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("card_comments")]
    public class CardComment : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("card_id")]
        public Guid CardId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}