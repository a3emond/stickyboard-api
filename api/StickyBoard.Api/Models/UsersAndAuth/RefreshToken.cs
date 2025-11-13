using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.UsersAndAuth
{
    [Table("refresh_tokens")]
    public class RefreshToken : IEntity
    {
        [Key, Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("client_id")]
        public string? ClientId { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("ip_addr")]
        public string? IpAddress { get; set; } // inet → string

        [Column("issued_at")]
        public DateTime IssuedAt { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("revoked")]
        public bool Revoked { get; set; }

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("replaced_by")]
        public string? ReplacedBy { get; set; }
    }
}