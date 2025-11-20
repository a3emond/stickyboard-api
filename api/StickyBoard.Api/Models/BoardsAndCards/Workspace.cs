using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("workspaces")]
public class Workspace : IEntity, ISoftDeletable
{
  [Column("id")]
  public Guid Id { get; set; }
  [Column("name")]
  public string Name { get; set; } = string.Empty;
  [Column("created_by")]
  public Guid CreatedBy { get; set; }
  [Column("created_at")]
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Will be set by the database default
  [Column("updated_at")]
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Will be set by the database default
  [Column("deleted_at")]
  public DateTime? DeletedAt { get; set; }
}
