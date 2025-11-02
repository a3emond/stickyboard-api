using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Models.BoardsAndCards
{
    [Table("permissions")]
    public class Permission: IEntity
    {
        [Key, Column("user_id", Order = 0)]
        public Guid UserId { get; set; }

        [Key, Column("board_id", Order = 1)]
        public Guid BoardId { get; set; }

        [Column("role")]
        public BoardRole Role { get; set; } = BoardRole.viewer;

        [Column("granted_at")]
        public DateTime GrantedAt { get; set; }
    }
}