namespace StickyBoard.Api.DTOs.Messaging
{
    public sealed class InviteCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public Guid? BoardId { get; set; }
        public Guid? OrganizationId { get; set; }
        // Role is optional; required when BoardId or OrganizationId is set
        public string? Role { get; set; } // "owner","editor","viewer" (BoardRole); will map to OrgRole when org invite
        public int? ExpiresInDays { get; set; } = 7;
    }

    public sealed class InvitePublicDto
    {
        public string Email { get; set; } = string.Empty;
        public Guid? BoardId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? Role { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Accepted { get; set; }
        public string SenderDisplayName { get; set; } = string.Empty;
    }

    public sealed class InviteListItemDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid? BoardId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? Role { get; set; }
        public bool Accepted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string SenderDisplayName { get; set; } = string.Empty;
    }

    public sealed class InviteRedeemRequestDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public sealed class InviteCreateResponseDto
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}