using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Models.Boards;

[Table("permissions")]
public class Permission : IEntity
{
    [Column("user_id")] public Guid UserId { get; set; }
    [Column("board_id")] public Guid BoardId { get; set; }
    [Column("role")] public BoardRole Role { get; set; }
    [Column("granted_at")] public DateTime GrantedAt { get; set; }
}