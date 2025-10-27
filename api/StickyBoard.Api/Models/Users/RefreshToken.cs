using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.Users;

[Table("refresh_tokens")]
public class RefreshToken : IEntity
{
    [Key, Column("token_hash")] public string TokenHash { get; set; } = string.Empty;
    [Column("user_id")] public Guid UserId { get; set; }
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("revoked")] public bool Revoked { get; set; }
}