using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Activities
{
    public sealed class ActivityDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public Guid? CardId { get; set; }
        public Guid? ActorId { get; set; }
        public ActivityType ActType { get; set; }
        public Dictionary<string, object> PayloadJson { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public sealed class CreateActivityDto
    {
        public Guid BoardId { get; set; }
        public Guid? CardId { get; set; }
        public ActivityType ActType { get; set; }
        public Dictionary<string, object>? PayloadJson { get; set; }
    }
}