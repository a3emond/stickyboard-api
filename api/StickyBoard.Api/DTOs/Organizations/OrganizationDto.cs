using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Organizations
{
    public sealed class OrganizationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateOrganizationDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class UpdateOrganizationDto
    {
        public string? Name { get; set; }
    }

    // ----------------------------------------------------------------------
    // MEMBERS
    // ----------------------------------------------------------------------

    public sealed class OrganizationMemberDto
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public OrgRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public sealed class AddMemberDto
    {
        public Guid UserId { get; set; }
        public OrgRole Role { get; set; } = OrgRole.member;
    }

    public sealed class UpdateMemberDto
    {
        public OrgRole Role { get; set; }
    }
}