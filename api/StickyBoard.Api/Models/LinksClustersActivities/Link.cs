using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.LinksClustersActivities;

[Table("links")]
public class Link
{
    [Key] public Guid Id { get; set; }
    [ForeignKey("FromCard")] public Guid FromCard { get; set; }
    [ForeignKey("ToCard")] public Guid ToCard { get; set; }
    [Column("rel_type")] public LinkType RelType { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [ForeignKey("Creator")] public Guid? CreatedBy { get; set; }
}