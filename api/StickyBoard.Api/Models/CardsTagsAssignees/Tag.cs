using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.CardsTagsAssignees;

[Table("tags")]
public class Tag
{
    [Key] public Guid Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
}