using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Clustering
{
    [Table("clusters")]
    public class Cluster : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("cluster_type")] public ClusterType ClusterType { get; set; }
        [Column("rule_def")] public JsonDocument? RuleDef { get; set; }
        [Column("visual_meta")] public JsonDocument VisualMeta { get; set; } = JsonDocument.Parse("{}");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    //Derived automatically by rule engine or similarity worker 
    
    //[Table("cluster_members")]
    //public class ClusterMember
    //{
    //    [Column("cluster_id")] public Guid ClusterId { get; set; }
    //    [Column("card_id")] public Guid CardId { get; set; }
    //}
    
    
    [Table("activities")]
    public class Activity : IEntity
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("card_id")] public Guid? CardId { get; set; }
        [Column("actor_id")] public Guid? ActorId { get; set; }
        [Column("act_type")] public ActivityType ActType { get; set; }
        [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }

    [Table("rules")]
    public class Rule : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("board_id")] public Guid BoardId { get; set; }
        [Column("definition")] public JsonDocument Definition { get; set; } = JsonDocument.Parse("{}");
        [Column("enabled")] public bool Enabled { get; set; } = true;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
