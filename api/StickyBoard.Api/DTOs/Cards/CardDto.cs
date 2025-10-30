using System;
using System.Collections.Generic;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Cards
{
    public sealed class CardDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? TabId { get; set; }
        public CardType Type { get; set; } = CardType.note;
        public string? Title { get; set; }
        public Dictionary<string, object>? ContentJson { get; set; }
        public Dictionary<string, object>? InkDataJson { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Priority { get; set; }
        public CardStatus Status { get; set; } = CardStatus.open;
        public Guid? AssigneeId { get; set; }
        public Guid? CreatedBy { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateCardDto
    {
        public Guid BoardId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? TabId { get; set; }
        public CardType Type { get; set; } = CardType.note;
        public string? Title { get; set; }
        public Dictionary<string, object>? ContentJson { get; set; }
        public Dictionary<string, object>? InkDataJson { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Priority { get; set; }
    }

    public sealed class UpdateCardDto
    {
        public Guid? SectionId { get; set; }
        public Guid? TabId { get; set; }
        public string? Title { get; set; }
        public Dictionary<string, object>? ContentJson { get; set; }
        public Dictionary<string, object>? InkDataJson { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Priority { get; set; }
        public CardStatus? Status { get; set; }
        public Guid? AssigneeId { get; set; }
    }

    public sealed class BulkAssignDto
    {
        public Guid AssigneeId { get; set; }
        public IEnumerable<Guid> CardIds { get; set; } = [];
    }
}
