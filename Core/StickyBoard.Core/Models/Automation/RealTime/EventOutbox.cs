using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Core.Models.Automation.RealTime;

[Table("event_outbox")]
public class EventOutbox
{
    [Column("cursor")] public long Cursor { get; set; }

    [Column("topic")] public OutboxTopic Topic { get; set; }

    [Column("entity_id")] public Guid EntityId { get; set; }

    [Column("workspace_id")] public Guid? WorkspaceId { get; set; }

    [Column("board_id")] public Guid? BoardId { get; set; }

    [Column("op")] public OutboxOperation Op { get; set; }

    [Column("payload")] public string Payload { get; set; } = null!;

    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
