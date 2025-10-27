using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Cards;

[Table("card_tags")]
public class CardTag : IEntity
{
    [Column("card_id")] public Guid CardId { get; set; }
    [Column("tag_id")] public Guid TagId { get; set; }
}