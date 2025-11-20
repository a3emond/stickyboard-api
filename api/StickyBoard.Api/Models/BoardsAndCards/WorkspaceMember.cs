using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("workspace_members")]
public class WorkspaceMember : IEntity
{
  [Key, Column("workspace_id", Order = 0)]
  public Guid WorkspaceId { get; set; }
  [Key, Column("user_id", Order = 1)]
  public Guid UserId { get; set; }
  [Column("role")]
  public WorkspaceRole Role { get; set; } = WorkspaceRole.member; // Database trigger will set creator as owner
  [Column("joined_at")]
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow; // Will be set by the database default
}
