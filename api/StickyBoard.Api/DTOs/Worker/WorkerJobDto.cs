using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Worker
{
    // ------------------------------------------------------------
    // REQUEST
    // ------------------------------------------------------------
    public sealed class EnqueueWorkerJobDto
    {
        public JobKind Kind { get; set; }
        public object Payload { get; set; } = new { };
        public short Priority { get; set; } = 0;
        public short MaxAttempts { get; set; } = 3;
    }

    // ------------------------------------------------------------
    // JOB DTO
    // ------------------------------------------------------------
    public sealed class WorkerJobDto
    {
        public Guid Id { get; set; }
        public JobKind JobKind { get; set; }
        public short Priority { get; set; }
        public DateTime RunAt { get; set; }
        public short MaxAttempts { get; set; }
        public short Attempt { get; set; }
        public JobStatus Status { get; set; }
        public string? PayloadJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ------------------------------------------------------------
    // JOB ATTEMPT DTO
    // ------------------------------------------------------------
    public sealed class WorkerJobAttemptDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? Error { get; set; }
    }

    // ------------------------------------------------------------
    // DEADLETTER DTO
    // ------------------------------------------------------------
    public sealed class WorkerJobDeadletterDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string JobKind { get; set; } = string.Empty;
        public string? PayloadJson { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
