using System;
using System.ComponentModel;
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

        [Column("board_id")]
        public Guid? BoardId { get; set; }

        [Column("org_id")]
        public Guid? OrgId { get; set; }

        [Column("board_role")]
        public BoardRole? BoardRole { get; set; }

        [Column("org_role")]
        public OrgRole? OrgRole { get; set; }

        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Column("accepted")]
        public bool Accepted { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }

}