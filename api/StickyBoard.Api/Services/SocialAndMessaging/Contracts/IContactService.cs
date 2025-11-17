using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface IContactService
{
    // ============================================================
    // LIST CONTACTS (with joined user details)
    // ============================================================
    Task<List<ContactEntryDto>> GetContactsAsync(
        Guid userId,
        ContactStatus? status,
        CancellationToken ct);

    // ============================================================
    // SEND CONTACT INVITE (delegates to InviteService)
    // ============================================================
    Task<InviteCreateResponseDto> SendContactInviteAsync(
        Guid senderId,
        Guid targetUserId,
        CancellationToken ct);

    // ============================================================
    // ACCEPT CONTACT (post-invite)
    // ============================================================
    Task AcceptContactAsync(
        Guid userId,
        Guid otherUserId,
        CancellationToken ct);

    // ============================================================
    // BLOCK / UNBLOCK
    // ============================================================
    Task BlockContactAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct);

    Task UnblockContactAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct);

    // ============================================================
    // DELETE CONTACT (reciprocal)
    // ============================================================
    Task<bool> DeleteContactAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct);
}