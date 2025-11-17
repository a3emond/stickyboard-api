using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Repositories.UsersAndAuth.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class ContactService : IContactService
{
    private readonly IContactRepository _contacts;
    private readonly IUserRepository _users;
    private readonly IInviteService _invites;

    public ContactService(
        IContactRepository contacts,
        IUserRepository users,
        IInviteService invites)
    {
        _contacts = contacts;
        _users = users;
        _invites = invites;
    }

    // ============================================================
    // LIST CONTACTS WITH PROFILE
    // ============================================================
    public async Task<List<ContactEntryDto>> GetContactsAsync(
        Guid userId,
        ContactStatus? status,
        CancellationToken ct)
    {
        IEnumerable<UserContact> list;

        if (status.HasValue)
        {
            list = await _contacts.GetContactsByUserIdAndStatusAsync(
                userId, status.Value, ct);
        }
        else
        {
            var accepted = await _contacts.GetContactsByUserIdAndStatusAsync(userId, ContactStatus.accepted, ct);
            var pending  = await _contacts.GetContactsByUserIdAndStatusAsync(userId, ContactStatus.pending, ct);
            var blocked  = await _contacts.GetContactsByUserIdAndStatusAsync(userId, ContactStatus.blocked, ct);
            list = accepted.Concat(pending).Concat(blocked);
        }

        var result = new List<ContactEntryDto>(list.Count());

        foreach (var c in list)
        {
            var u = await _users.GetByIdAsync(c.ContactId, ct);
            if (u is null) continue;

            result.Add(new ContactEntryDto
            {
                UserId      = u.Id,
                DisplayName = u.DisplayName,
                AvatarUrl   = u.AvatarUri,
                Status      = c.Status,
                CreatedAt   = c.CreatedAt,
                AcceptedAt  = c.AcceptedAt
            });
        }

        return result;
    }


    // ============================================================
    // SEND CONTACT REQUEST (InviteService)
    // ============================================================
    public async Task<InviteCreateResponseDto> SendContactInviteAsync(
        Guid senderId,
        Guid targetUserId,
        CancellationToken ct)
    {
        if (senderId == targetUserId)
            throw new ValidationException("Cannot add yourself.");

        // Prevent duplicates or spamming
        if (await _contacts.ContactExistsAsync(senderId, targetUserId, ct))
            throw new ConflictException("Contact already exists or invitation pending.");

        var target = await _users.GetByIdAsync(targetUserId, ct)
                     ?? throw new NotFoundException("Target user not found.");

        // Build the invite request
        var request = new InviteCreateRequestDto
        {
            SenderId    = senderId,
            Email       = target.Email,
            Scope       = InviteScope.Contact,
            ContactId   = targetUserId,
            WorkspaceId = null,
            BoardId     = null,
            TargetRole  = null,
            BoardRole   = null,
            ExpiresIn   = TimeSpan.FromDays(7),
            Note        = null
        };

        // Use InviteService to create the invite
        return await _invites.CreateAsync(request, ct);
    }


    // ============================================================
    // ACCEPT CONTACT (called after InviteService.AcceptAsync)
    // ============================================================
    public async Task AcceptContactAsync(Guid userId, Guid otherUserId, CancellationToken ct)
    {
        // Post-invite acceptance sanity check
        if (!await _contacts.ContactExistsAsync(userId, otherUserId, ct))
            throw new NotFoundException("Contact relationship missing after invite acceptance.");

        // Mutual acceptance with proper timestamp update
        await _contacts.UpdateContactStatusAsync(userId, otherUserId, ContactStatus.accepted, ct);
        await _contacts.UpdateContactStatusAsync(otherUserId, userId, ContactStatus.accepted, ct);
    }


    // ============================================================
    // BLOCK
    // ============================================================
    public async Task BlockContactAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        if (!await _contacts.ContactExistsAsync(userId, contactId, ct))
            throw new NotFoundException("Contact relation not found.");

        await _contacts.UpdateContactStatusAsync(userId, contactId, ContactStatus.blocked, ct);
    }

    // ============================================================
    // UNBLOCK (return to pending)
    // ============================================================
    public async Task UnblockContactAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        if (!await _contacts.ContactExistsAsync(userId, contactId, ct))
            throw new NotFoundException("Contact relation not found.");

        // Decide whether it returns to "pending" or "accepted"
        // For now: pending, until the other accepts again
        await _contacts.UpdateContactStatusAsync(userId, contactId, ContactStatus.pending, ct);
    }


    // ============================================================
    // DELETE (reciprocal)
    // ============================================================
    public Task<bool> DeleteContactAsync(Guid userId, Guid contactId, CancellationToken ct)
        => _contacts.DeleteReciprocatedContactAsync(userId, contactId, ct);
}
