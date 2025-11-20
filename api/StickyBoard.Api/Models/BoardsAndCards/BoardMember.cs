using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards;

[Table("board_members")]
public sealed class BoardMember : IEntity
{
    [Key, Column("board_id", Order = 0)]
    public Guid BoardId { get; set; }

    [Key, Column("user_id", Order = 1)]
    public Guid UserId { get; set; }

    [Column("role")]
    public WorkspaceRole? Role { get; set; }
}