namespace StickyBoard.Api.DTOs.Users;

public sealed class UserRelationDto
{
    public Guid FriendId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserRelationCreateDto
{
    public Guid FriendId { get; set; }
}

public sealed class UserRelationUpdateDto
{
    public string Status { get; set; } = "active";
}

public sealed class UserRelationResponseDto
{
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}