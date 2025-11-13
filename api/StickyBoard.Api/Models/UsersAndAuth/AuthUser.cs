using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.UsersAndAuth
{
    [Table("auth_users")]
    public class AuthUser : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("user_id")]
        public Guid UserId { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role")]
        public UserRole Role { get; set; } = UserRole.user;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}