using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging
{
    [Table("invites")]
    public class Invite : IEntity
    {
        [Key, Column("id")]
        public Guid Id { get; set; }

        [Column("sender_id")]
        public Guid SenderId { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("scope_type")]
        public InviteScope ScopeType { get; set; }

        [Column("workspace_id")]
        public Guid? WorkspaceId { get; set; }

        [Column("board_id")]
        public Guid? BoardId { get; set; }

        [Column("contact_id")]
        public Guid? ContactId { get; set; }

        [Column("target_role")]
        public WorkspaceRole? TargetRole { get; set; }

        [Column("board_role")]
        public WorkspaceRole? BoardRole { get; set; }

        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Column("status")]
        public InviteStatus Status { get; set; }

        [Column("accepted_by")]
        public Guid? AcceptedBy { get; set; }

        [Column("accepted_at")]
        public DateTime? AcceptedAt { get; set; }

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("note")]
        public string? Note { get; set; }
    }
}