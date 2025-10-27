using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Cards
{
    // ------------------------
    // TAGS
    // ------------------------
    public sealed class TagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class CreateTagDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class CardTagDto
    {
        public Guid CardId { get; set; }
        public Guid TagId { get; set; }
    }

    public sealed class AssignTagDto
    {
        public IEnumerable<Guid> TagIds { get; set; } = [];
    }

    // ------------------------
    // LINKS
    // ------------------------
    public sealed class LinkDto
    {
        public Guid Id { get; set; }
        public Guid FromCard { get; set; }
        public Guid ToCard { get; set; }
        public LinkType RelType { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class CreateLinkDto
    {
        public Guid ToCard { get; set; }
        public LinkType RelType { get; set; } = LinkType.relates_to;
    }

    public sealed class UpdateLinkDto
    {
        public LinkType RelType { get; set; }
    }
}