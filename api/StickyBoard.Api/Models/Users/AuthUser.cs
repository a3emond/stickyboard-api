using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Api.Models.Users;

[Table("auth_users")]
public class AuthUser
{
    [Key, ForeignKey("User")]
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.user;
    [Column("last_login")] public DateTime? LastLogin { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}