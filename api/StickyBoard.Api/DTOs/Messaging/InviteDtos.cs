namespace StickyBoard.Api.DTOs.Messaging
{
    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public sealed class InviteCreateDto
    {
        public string Email { get; set; } = string.Empty;

        public Guid? BoardId { get; set; }

        public Guid? OrganizationId { get; set; }

        // Use explicit role fields for clarity and validation
        public string? BoardRole { get; set; } // "owner","editor","viewer"
        public string? OrgRole { get; set; }   // "owner","admin","member"

        public int? ExpiresInDays { get; set; } = 7;
    }

    // ------------------------------------------------------------
    // PUBLIC (Landing page view)
    // ------------------------------------------------------------
    public sealed class InvitePublicDto
    {
        public string Email { get; set; } = string.Empty;

        public Guid? BoardId { get; set; }

        public Guid? OrganizationId { get; set; }

        public string? BoardRole { get; set; }

        public string? OrgRole { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool Accepted { get; set; }

        public string SenderDisplayName { get; set; } = string.Empty;
    }

    // ------------------------------------------------------------
    // LIST ITEM (for sender or recipient)
    // ------------------------------------------------------------
    public sealed class InviteListItemDto
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public Guid? BoardId { get; set; }

        public Guid? OrganizationId { get; set; }

        public string? BoardRole { get; set; }

        public string? OrgRole { get; set; }

        public bool Accepted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public string SenderDisplayName { get; set; } = string.Empty;
    }

    // ------------------------------------------------------------
    // REDEEM REQUEST
    // ------------------------------------------------------------
    public sealed class InviteRedeemRequestDto
    {
        public string Token { get; set; } = string.Empty;
    }

    // ------------------------------------------------------------
    // CREATE RESPONSE
    // ------------------------------------------------------------
    public sealed class InviteCreateResponseDto
    {
        public Guid Id { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
