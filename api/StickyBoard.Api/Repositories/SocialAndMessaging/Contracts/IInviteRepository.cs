using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface IInviteRepository : IRepository<Invite>, ISyncRepository<Invite>
{
    // =====================================================================
    // DB FUNCTION HELPERS
    // =====================================================================

    Task<Guid> CreateViaDbFunctionAsync(
        Guid senderId,
        string email,
        InviteScope scopeType,
        Guid? workspaceId,
        Guid? boardId,
        Guid? contactId,
        WorkspaceRole? targetRole,
        WorkspaceRole? boardRole,
        string tokenHash,
        TimeSpan expiresIn,
        string? note,
        CancellationToken ct);

    Task<InviteAcceptResponseDto?> AcceptViaDbFunctionAsync(
        string tokenHash,
        Guid acceptingUserId,
        CancellationToken ct);

    Task<bool> RevokeViaDbFunctionAsync(
        string tokenHash,
        CancellationToken ct);


    // =====================================================================
    // QUERIES
    // =====================================================================

    Task<IEnumerable<Invite>> GetBySenderAsync(
        Guid senderId,
        CancellationToken ct);

    Task<IEnumerable<Invite>> GetByEmailAsync(
        string email,
        CancellationToken ct);

    Task<Invite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken ct);


    // =====================================================================
    // VALIDATION HELPERS
    // =====================================================================

    Task<bool> ValidateWorkspaceExistsAsync(
        Guid workspaceId,
        CancellationToken ct);

    Task<bool> ValidateBoardExistsAsync(
        Guid boardId,
        CancellationToken ct);
}