using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.SocialAndMessaging;
[Table("user_contacts")]
public class UserContact : IEntityUpdatable
{

    [Key, Column("user_id", Order = 0)]
    public Guid UserId { get; set; }
    [Key, Column("contact_id", Order = 1)]
    public Guid ContactId { get; set; }
    [Column("status")]
    public ContactStatus Status { get; set; } = ContactStatus.pending;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("accepted_at")]
    public DateTime? AcceptedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } 
}