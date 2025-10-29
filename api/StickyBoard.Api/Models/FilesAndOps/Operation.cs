using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.FilesAndOps
{
    [Table("operations")]
    public sealed class Operation : IEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; }

        [Column("device_id")]
        [Required]
        public string? DeviceId { get; init; } = string.Empty;

        [Column("user_id")]
        [Required]
        public Guid UserId { get; init; }

        [Column("entity")]
        [Required]
        public EntityType Entity { get; init; }

        [Column("entity_id")]
        [Required]
        public Guid EntityId { get; init; }

        [Column("op_type")]
        [Required]
        public string OpType { get; init; } = string.Empty;

        [Column("payload")]
        [Required]
        public JsonDocument Payload { get; init; } = JsonDocument.Parse("{}");

        [Column("version_prev")]
        public int? VersionPrev { get; init; }

        [Column("version_next")]
        public int? VersionNext { get; init; }

        [Column("created_at")]
        public DateTime CreatedAt { get; init; }
    }
}