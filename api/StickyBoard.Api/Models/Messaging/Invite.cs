using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models
{
    [Table("invites")]
    public sealed class Invite : IEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; }

        [Column("sender_id")]
        [Required]
        public Guid SenderId { get; init; }

        [Column("email")]
        [Required]
        public string Email { get; init; } = string.Empty;

        [Column("board_id")]
        public Guid? BoardId { get; init; }

        [Column("org_id")]
        public Guid? OrgId { get; init; }

        [Column("board_role")]
        public BoardRole? BoardRole { get; init; }

        [Column("org_role")]
        public OrgRole? OrgRole { get; init; }

        [Column("token")]
        [Required]
        public string Token { get; init; } = string.Empty;

        [Column("accepted")]
        public bool Accepted { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; init; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; init; }

        [NotMapped]
        public bool IsBoardInvite => BoardId.HasValue;

        [NotMapped]
        public bool IsOrgInvite => OrgId.HasValue;

        [NotMapped]
        public bool IsFriendInvite => !IsBoardInvite && !IsOrgInvite;
    }
}