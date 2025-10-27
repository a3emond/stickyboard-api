using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Permissions
{
    public sealed class PermissionDto
    {
        public Guid UserId { get; set; }
        public Guid BoardId { get; set; }
        public BoardRole Role { get; set; }
        public DateTime GrantedAt { get; set; }
    }

    public sealed class GrantPermissionDto
    {
        public Guid UserId { get; set; }
        public BoardRole Role { get; set; } = BoardRole.viewer;
    }

    public sealed class UpdatePermissionDto
    {
        public BoardRole Role { get; set; }
    }
}