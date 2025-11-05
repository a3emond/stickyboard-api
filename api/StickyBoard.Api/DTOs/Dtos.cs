// Dtos.cs

using StickyBoard.Api.Models;

namespace StickyBoard.Api.DTOs
{
    // ==========================================================
    // 0) API Envelopes
    // ==========================================================
    public sealed class ApiResponseDto<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }

        public static ApiResponseDto<T> Ok(T data, string? message = null)
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponseDto<T> Fail(string message)
            => new() { Success = false, Message = message, Data = default };
    }

    public sealed class ErrorDto
    {
        public ErrorCode Code { get; init; } = ErrorCode.SERVER_ERROR; // AUTH_INVALID, FORBIDDEN, etc.
        public string Message { get; init; } = string.Empty;
        public string? Details { get; init; }
    }

    // ==========================================================
    // 1) Users & Auth
    // ==========================================================
    public sealed class UserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public UserRole Role { get; init; }
    }

    public sealed class UserSelfDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public object? Prefs { get; init; } = new { };
        public DateTime CreatedAt { get; init; }
    }

    public sealed class UserUpdateDto
    {
        public string? DisplayName { get; init; }
        public string? AvatarUrl { get; init; }
        public object? Prefs { get; init; }
    }

    public sealed class ChangePasswordDto
    {
        public string OldPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }

    public sealed class AuthLoginRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public sealed class AuthLoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public UserSelfDto User { get; init; } = new();
    }

    public sealed class AuthRefreshRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }

    public sealed class AuthRefreshResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }

    public sealed class RegisterRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? InviteToken { get; init; }
    }

    public sealed class RegisterResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public UserSelfDto User { get; init; } = new();
    }

    // ==========================================================
    // 2) Organizations
    // ==========================================================
    public sealed class OrganizationDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid OwnerId { get; init; }
    }

    public sealed class OrganizationCreateDto
    {
        public string Name { get; init; } = string.Empty;
    }

    public sealed class OrganizationUpdateDto
    {
        public string Name { get; init; } = string.Empty;
    }

    public sealed class OrganizationMemberDto
    {
        public UserDto User { get; init; } = new();
        public OrgRole Role { get; init; }
    }
    
    // NOT USED CURRENTLY
    /*
    public sealed class OrganizationInviteMemberDto
    {
        public Guid UserId { get; init; }
        public OrgRole Role { get; init; } = OrgRole.member;
    }
    
    public sealed class OrganizationUpdateMemberRoleDto
    {
        public OrgRole Role { get; init; }
    }

    */

    // ==========================================================
    // 3) Board Folders
    // ==========================================================
    public sealed class BoardFolderDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid? OrgId { get; init; }
        public Guid? UserId { get; init; }
        public string? Icon { get; init; }
        public string? Color { get; init; }
        public object? Meta { get; init; } = new { };
    }

    public sealed class BoardFolderCreateDto
    {
        public string Name { get; init; } = string.Empty;
        public Guid? OrgId { get; init; }
        public string? Icon { get; init; }
        public string? Color { get; init; }
        public object? Meta { get; init; }
    }

    public sealed class BoardFolderUpdateDto
    {
        public string? Name { get; init; }
        public string? Icon { get; init; }
        public string? Color { get; init; }
        public object? Meta { get; init; }
    }

    // ==========================================================
    // 4) Boards
    // ==========================================================
    public sealed class BoardDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public BoardVisibility Visibility { get; init; }
        public Guid OwnerId { get; init; }
        public Guid? OrgId { get; init; }
        public Guid? FolderId { get; init; }
        public object? Theme { get; init; } = new { };
        public object? Meta { get; init; } = new { };
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed class BoardCreateDto
    {
        public string Title { get; init; } = string.Empty;
        public BoardVisibility Visibility { get; init; }
        public Guid? OrgId { get; init; }
        public Guid? FolderId { get; init; }
        public object? Theme { get; init; }
        public object? Meta { get; init; }
    }

    public sealed class BoardUpdateDto
    {
        public string? Title { get; init; }
        public BoardVisibility? Visibility { get; init; }
        public Guid? FolderId { get; init; }
        public object? Theme { get; init; }
        public object? Meta { get; init; }
    }

    public sealed class RenameBoardDto
    {
        public string Title { get; init; } = string.Empty;
    }

    public sealed class MoveBoardFolderDto
    {
        public Guid? FolderId { get; init; }
    }

    public sealed class MoveBoardOrgDto
    {
        public Guid? OrgId { get; init; }
    }

    // Canonical permission DTOs (board-level)
    public sealed class PermissionDto
    {
        public Guid UserId { get; init; }
        public Guid BoardId { get; init; }
        public BoardRole Role { get; init; }
        public DateTime GrantedAt { get; init; }
    }

    public sealed class GrantPermissionDto
    {
        public Guid UserId { get; init; }
        public BoardRole Role { get; init; }
    }

    public sealed class UpdatePermissionDto
    {
        public BoardRole Role { get; init; }
    }

    // ==========================================================
    // 5) Tabs
    // ==========================================================
    public sealed class TabDto
    {
        public Guid Id { get; init; }
        public Guid BoardId { get; init; }
        public string Title { get; init; } = string.Empty;
        public TabType TabType { get; init; }
        public int Position { get; init; }
        public object? Layout { get; init; } = new { };
    }

    public sealed class TabCreateDto
    {
        public Guid BoardId { get; init; }
        public string Title { get; init; } = string.Empty;
        public TabType TabType { get; init; }
        public int Position { get; init; }
        public object? Layout { get; init; }
    }

    public sealed class TabUpdateDto
    {
        public string? Title { get; init; }
        public TabType? TabType { get; init; }
        public int Position { get; init; }
        public object? Layout { get; init; }
    }
    
    public sealed class TabMoveDto
    {
        public int NewPosition { get; init; }
    }


    // ==========================================================
    // 6) Sections
    // ==========================================================
    public sealed class SectionDto
    {
        public Guid Id { get; init; }
        public Guid TabId { get; init; }
        public Guid? ParentSectionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public int Position { get; init; }
        public object? Layout { get; init; } = new { };
    }

    public sealed class SectionCreateDto
    {
        public Guid TabId { get; init; }
        public Guid? ParentSectionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public int Position { get; init; }
        public object? Layout { get; init; }
    }

    public sealed class SectionUpdateDto
    {
        public string? Title { get; init; }
        public int Position { get; init; }
        public Guid? ParentSectionId { get; init; }
        public object? Layout { get; init; }
    }

    public sealed class SectionMoveDto
    {
        public int NewPosition { get; init; }
        public Guid? ParentSectionId { get; init; }
    } 
    // ==========================================================
    // 7) Cards
    // ==========================================================
    public sealed class CardDto
    {
        public Guid Id { get; init; }
        public Guid BoardId { get; init; }
        public Guid TabId { get; init; }
        public Guid? SectionId { get; init; }
        public CardType Type { get; init; }
        public string? Title { get; init; }
        public object? Content { get; init; } = new { };
        public object? InkData { get; init; }
        public List<string> Tags { get; init; } = new();
        public CardStatus Status { get; init; }
        public int Priority { get; init; }
        public int Position { get; init; }      
        public Guid? AssigneeId { get; init; }
        public DateTime? DueDate { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed class CardCreateDto
    {
        public Guid BoardId { get; init; }
        public Guid TabId { get; init; }
        public Guid? SectionId { get; init; }
        public CardType Type { get; init; }
        public string? Title { get; init; }
        public object? Content { get; init; }
        public object? InkData { get; init; }
        public List<string>? Tags { get; init; }
        public int Priority { get; init; }
        public Guid? AssigneeId { get; init; }
        public DateTime? DueDate { get; init; }
        public int? Position { get; init; }      
    }

    public sealed class CardUpdateDto
    {
        public string? Title { get; init; }
        public object? Content { get; init; }
        public object? InkData { get; init; }
        public List<string>? Tags { get; init; }
        public CardStatus? Status { get; init; }
        public int Priority { get; init; }
        public Guid? AssigneeId { get; init; }
        public DateTime? DueDate { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
        public Guid? SectionId { get; init; }
        public Guid? TabId { get; init; }
        public int? Position { get; init; }      
    }

    public sealed class CardReorderDto
    {
        public Guid CardId { get; init; }
        public int Position { get; init; }
    }

    public sealed class CardReorderRequest
    {
        public List<CardReorderDto> Updates { get; init; } = new();
    }

    // ==========================================================
    // 8) Card Comments
    // ==========================================================
    public sealed class CardCommentDto
    {
        public Guid Id { get; init; }
        public Guid CardId { get; init; }
        public UserDto User { get; init; } = new();
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class CardCommentCreateDto
    {
        public string Content { get; init; } = string.Empty;
    }

    // ==========================================================
    // 9) Board Chat
    // ==========================================================
    public sealed class BoardMessageDto
    {
        public Guid Id { get; init; }
        public Guid BoardId { get; init; }
        public UserDto User { get; init; } = new();
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class BoardMessageCreateDto
    {
        public string Content { get; init; } = string.Empty;
    }

    // ==========================================================
    // 10) Social — User Relations
    // ==========================================================
    public sealed class UserRelationDto
    {
        public Guid UserId { get; init; }
        public Guid FriendId { get; init; }
        public RelationStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed class UserRelationCreateDto
    {
        public Guid FriendId { get; init; }
    }

    public sealed class UserRelationUpdateDto
    {
        public RelationStatus Status { get; init; }
    }

    // ==========================================================
    // 11) Direct Messages (Social & Messaging)
    //   — Using ids (no embedded User in DTO), plus related ids
    // ==========================================================
    public sealed class MessageDto
    {
        public Guid Id { get; init; }
        public Guid? SenderId { get; init; }
        public Guid ReceiverId { get; init; }
        public string? Subject { get; init; }
        public string Body { get; init; } = string.Empty;
        public MessageType Type { get; init; } = MessageType.direct;
        public Guid? RelatedBoardId { get; init; }
        public Guid? RelatedOrgId { get; init; }
        public MessageStatus Status { get; init; } = MessageStatus.unread;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class SendMessageDto
    {
        public Guid ReceiverId { get; init; }
        public string? Subject { get; init; }
        public string Body { get; init; } = string.Empty;
        public MessageType Type { get; init; } = MessageType.direct;
        public Guid? RelatedBoardId { get; init; }
        public Guid? RelatedOrgId { get; init; }
    }

    public sealed class UpdateMessageStatusDto
    {
        public MessageStatus Status { get; init; }
    }

    // ==========================================================
    // 12) Invites
    // ==========================================================
    public sealed class InviteDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public Guid? BoardId { get; init; }
        public Guid? OrgId { get; init; }
        public BoardRole? BoardRole { get; init; }
        public OrgRole? OrgRole { get; init; }
        public bool Accepted { get; init; }
        public DateTime ExpiresAt { get; init; }
    }

    public sealed class InviteCreateDto
    {
        public string Email { get; init; } = string.Empty;
        public Guid? BoardId { get; init; }
        public Guid? OrgId { get; init; }
        public BoardRole? BoardRole { get; init; }
        public OrgRole? OrgRole { get; init; }
        public int? ExpiresInDays { get; init; } // default server-side 30 days
    }

    public sealed class InviteCreateResponseDto
    {
        public Guid Id { get; init; }
        public string Token { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }

    public sealed class InviteRedeemRequestDto
    {
        public string Token { get; init; } = string.Empty;
    }

    public sealed class InviteListItemDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public Guid? BoardId { get; init; }
        public Guid? OrganizationId { get; init; }
        public BoardRole? BoardRole { get; init; }
        public OrgRole? OrgRole { get; init; }
        public bool Accepted { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public string SenderDisplayName { get; init; } = string.Empty;
    }

    public sealed class InvitePublicDto
    {
        public string Email { get; init; } = string.Empty;
        public Guid? BoardId { get; init; }
        public Guid? OrganizationId { get; init; }
        public BoardRole? BoardRole { get; init; }
        public OrgRole? OrgRole { get; init; }
        public bool Accepted { get; init; }
        public DateTime ExpiresAt { get; init; }
        public string SenderDisplayName { get; init; } = string.Empty;
    }
}
