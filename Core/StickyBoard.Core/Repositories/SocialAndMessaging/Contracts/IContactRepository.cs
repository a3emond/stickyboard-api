using StickyBoard.Core.Models;
using StickyBoard.Core.Models.SocialAndMessaging;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;

public interface IContactRepository : IRepository<UserContact>, ISyncRepository<UserContact>
{
    // update status of contact
    Task UpdateContactStatusAsync(Guid userId, Guid contactId, ContactStatus status, CancellationToken ct);

    // get contacts by user id and status
    Task<List<UserContact>> GetContactsByUserIdAndStatusAsync(Guid userId, ContactStatus status, CancellationToken ct);

    // check if contact exists
    Task<bool> ContactExistsAsync(Guid userId, Guid contactId, CancellationToken ct);

    //the hard delete must be reciprocated (both sides deletion)
    Task<bool> DeleteReciprocatedContactAsync(Guid userId, Guid contactId, CancellationToken ct);

    // add new contact must go through invite process
}