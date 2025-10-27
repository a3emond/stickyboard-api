using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Cards;

[Table("links")]
public class Link : IEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("from_card")] public Guid FromCard { get; set; }
    [Column("to_card")] public Guid ToCard { get; set; }
    [Column("rel_type")] public LinkType RelType { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("created_by")] public Guid? CreatedBy { get; set; }
}