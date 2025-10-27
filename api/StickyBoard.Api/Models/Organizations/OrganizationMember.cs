using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Organizations;

[Table("organization_members")]
public class OrganizationMember : IEntity
{
    [Key, Column("org_id", Order = 0)] public Guid OrganizationId { get; set; }
    [Key, Column("user_id", Order = 1)] public Guid UserId { get; set; }
    [Column("role")] public OrgRole Role { get; set; } = OrgRole.member;
    [Column("joined_at")] public DateTime JoinedAt { get; set; }
    
}