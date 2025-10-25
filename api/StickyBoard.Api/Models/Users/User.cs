using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Users
{
    [Table("users")]
    public class User : IEntityUpdatable
    {
        [Key, Column("id")] public Guid Id { get; set; }
        [Column("email")] public string Email { get; set; } = string.Empty;
        [Column("display_name")] public string DisplayName { get; set; } = string.Empty;
        [Column("avatar_uri")] public string? AvatarUri { get; set; }
        [Column("prefs")] public JsonDocument Prefs { get; set; } = JsonDocument.Parse("{}");
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Table("auth_users")]
    public class AuthUser : IEntityUpdatable
    {
        [Key, Column("user_id")] public Guid Id { get; set; }
        [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
        [Column("role")] public UserRole Role { get; set; } = UserRole.user;
        [Column("last_login")] public DateTime? LastLogin { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
    
    [Table("refresh_tokens")]
    public class RefreshToken : IEntity
    {
        [Key, Column("token_hash")] public string TokenHash { get; set; } = string.Empty;
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("expires_at")] public DateTime ExpiresAt { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("revoked")] public bool Revoked { get; set; }
    }
}