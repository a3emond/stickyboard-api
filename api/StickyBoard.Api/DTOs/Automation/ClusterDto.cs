using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Automation
{
    public sealed class ClusterDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public ClusterType ClusterType { get; set; }
        public string? RuleDefJson { get; set; }
        public string VisualMetaJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateClusterDto
    {
        public ClusterType ClusterType { get; set; }
        public string? RuleDefJson { get; set; }
        public string VisualMetaJson { get; set; } = "{}";
    }

    public sealed class UpdateClusterDto
    {
        public ClusterType? ClusterType { get; set; }
        public string? RuleDefJson { get; set; }
        public string? VisualMetaJson { get; set; }
    }
}