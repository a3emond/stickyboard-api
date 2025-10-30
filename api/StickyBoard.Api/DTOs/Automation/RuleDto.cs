using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Automation
{
    public sealed class RuleDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public Dictionary<string, object> DefinitionJson { get; set; } = new();
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateRuleDto
    {
        public Dictionary<string, object>? DefinitionJson { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public sealed class UpdateRuleDto
    {
        public Dictionary<string, object>? DefinitionJson { get; set; }
        public bool? Enabled { get; set; }
    }
}