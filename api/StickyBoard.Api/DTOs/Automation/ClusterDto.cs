using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Automation
{
    public sealed class ClusterDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public ClusterType ClusterType { get; set; }
        public Dictionary<string, object>? RuleDefJson { get; set; }
        public Dictionary<string, object> VisualMetaJson { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateClusterDto
    {
        public ClusterType ClusterType { get; set; }
        public Dictionary<string, object>? RuleDefJson { get; set; }
        public Dictionary<string, object> VisualMetaJson { get; set; } = new();
    }

    public sealed class UpdateClusterDto
    {
        public ClusterType? ClusterType { get; set; }
        public Dictionary<string, object>? RuleDefJson { get; set; }
        public Dictionary<string, object>? VisualMetaJson { get; set; }
    }
}