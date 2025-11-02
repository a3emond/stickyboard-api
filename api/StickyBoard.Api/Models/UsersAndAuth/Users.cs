using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.UsersAndAuth
{
    [Table("users")]
    public class User : IEntityUpdatable, ISoftDeletable
    {
        [Key, Column("id")] 
        public Guid Id { get; set; }

        [Column("email")] 
        public string Email { get; set; } = string.Empty;

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

        [Column("deleted_at")] 
        public DateTime? DeletedAt { get; set; }
    }
}