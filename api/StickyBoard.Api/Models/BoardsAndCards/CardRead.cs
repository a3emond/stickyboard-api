using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("card_reads")]
public class CardRead : IEntity
{
    [Key, Column("card_id", Order = 0)]
    public Guid CardId { get; set; }

    [Key, Column("user_id", Order = 1)]
    public Guid UserId { get; set; }

    [Column("last_seen_at")]
    public DateTime LastSeenAt { get; set; }
}