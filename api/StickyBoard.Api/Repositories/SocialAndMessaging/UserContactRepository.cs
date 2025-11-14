using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public class UserContactRepository : RepositoryBase<UserContact>, IUserContactRepository
{
    public UserContactRepository(NpgsqlDataSource db) : base(db) { }


    protected override UserContact Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<UserContact>(r);

    public override Task<Guid> CreateAsync(UserContact entity, CancellationToken ct)
    {
        throw new ForbiddenException("Direct creation of UserContact is not allowed. Use the invite process instead.");
    }

    public override Task<bool> UpdateAsync(UserContact entity, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task UpdateContactStatusAsync(Guid userId, Guid contactId, ContactStatus status, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserContact>> GetContactsByUserIdAndStatusAsync(Guid userId, ContactStatus status, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ContactExistsAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteReciprocatedContactAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        //the hard delete must be reciprocated (both sides deletion)
        // first delete (userId, contactId)
        // then delete (contactId, userId)
        throw new NotImplementedException();
    }
    
    //Delete async is to be overriden to prevent direct deletion by calling DeleteReciprocatedContactAsync
    public override Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        throw new ForbiddenException("Direct deletion of UserContact is not allowed. Use DeleteReciprocatedContactAsync instead.");
    }
    
    // TODO: add methods with join tables to filter contacts by workspace/board/groups


}