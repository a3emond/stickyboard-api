using System;

namespace StickyBoard.Api.DTOs.Automation
{
    public sealed class RuleDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string DefinitionJson { get; set; } = "{}";
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateRuleDto
    {
        public string DefinitionJson { get; set; } = "{}";
        public bool Enabled { get; set; } = true;
    }

    public sealed class UpdateRuleDto
    {
        public string? DefinitionJson { get; set; }
        public bool? Enabled { get; set; }
    }
}