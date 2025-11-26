using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Core.Models.Base;

namespace StickyBoard.Core.Models.Automation.Jobs;

[Table("worker_jobs")]
public class WorkerJob : IEntityUpdatable
{
    [Column("id")] public long Id { get; set; }

    [Column("kind")] public WorkerJobKind Kind { get; set; }

    [Column("payload")] public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    [Column("status")] public WorkerJobStatus Status { get; set; }

    [Column("priority")] public int Priority { get; set; }

    [Column("attempts")] public int Attempts { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("available_at")] public DateTime AvailableAt { get; set; }

    [Column("last_error")] public string? LastError { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
