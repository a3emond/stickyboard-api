using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.LinksClustersActivities;

[Table("cluster_members")]
public class ClusterMember
{
    [Key, Column(Order = 0)] public Guid ClusterId { get; set; }
    [Key, Column(Order = 1)] public Guid CardId { get; set; }
}