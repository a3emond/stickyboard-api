using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StickyBoard.Api.Models.Users;

[Table("users")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("avatar_uri")]
    public string? AvatarUri { get; set; }

    [Column("prefs")]
    public JsonDocument Prefs { get; set; } = JsonDocument.Parse("{}");

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public AuthUser? Auth { get; set; }
}