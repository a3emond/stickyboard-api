using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

public interface IUserContactRepository : IRepository<UserContact>, ISyncRepository<UserContact>
{
    // update status of contact
    Task UpdateContactStatusAsync(Guid userId, Guid contactId, ContactStatus status, CancellationToken ct);
    
    // get contacts by user id and status
    Task<List<UserContact>> GetContactsByUserIdAndStatusAsync(Guid userId, ContactStatus status, CancellationToken ct);
    
    // check if contact exists
    Task<bool> ContactExistsAsync(Guid userId, Guid contactId, CancellationToken ct);
    
    //the hard delete must be reciprocated (both sides deletion)
    Task <bool> DeleteReciprocatedContactAsync(Guid userId, Guid contactId, CancellationToken ct);
    
    // add new contact must go through invite process
}