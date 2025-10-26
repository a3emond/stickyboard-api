using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Organizations;

[Table("organizations")]
public class Organization : IEntityUpdatable
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("owner_id")] public Guid OwnerId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}


[Table("organization_members")]
public class OrganizationMember : IEntity
{
    [Key, Column("org_id", Order = 0)] public Guid OrganizationId { get; set; }
    [Key, Column("user_id", Order = 1)] public Guid UserId { get; set; }
    [Column("role")] public OrgRole Role { get; set; } = OrgRole.member;
    [Column("joined_at")] public DateTime JoinedAt { get; set; }
    
}