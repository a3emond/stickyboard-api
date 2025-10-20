using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.CardsTagsAssignees;

[Table("card_tags")]
public class CardTag
{
    [Key, Column(Order = 0)] public Guid CardId { get; set; }
    [Key, Column(Order = 1)] public Guid TagId { get; set; }
}