using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Users;

namespace StickyBoard.Api.Models.Boards;

[Table("permissions")]
public class Permission
{
    [Key, Column(Order = 0)] public Guid UserId { get; set; }
    [Key, Column(Order = 1)] public Guid BoardId { get; set; }
    public BoardRole Role { get; set; }
    [Column("granted_at")] public DateTime GrantedAt { get; set; }

    public User? User { get; set; }
    public Board? Board { get; set; }
}