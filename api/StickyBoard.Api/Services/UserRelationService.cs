using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging;
using StickyBoard.Api.Repositories.UsersAndAuth;

namespace StickyBoard.Api.Services;

public sealed class UserRelationService
{
    private readonly UserRelationRepository _relations;
    private readonly UserRepository _users;

    public UserRelationService(UserRelationRepository relations, UserRepository users)
    {
        _relations = relations;
        _users = users;
    }

    // ----------------------------------------------------------------------
    // CREATE RELATION (two-way)
    // ----------------------------------------------------------------------
    public async Task<bool> CreateAsync(Guid userId, Guid friendId, CancellationToken ct)
    {
        if (userId == friendId)
            throw new ValidationException("Cannot create a relation with yourself.");

        var existing = await _relations.GetAsync(userId, friendId, ct);
        if (existing is not null && existing.Status == RelationStatus.active_)
            return true;

        var userExists = await _users.GetByIdAsync(friendId, ct);
        if (userExists is null)
            throw new NotFoundException("Friend user not found.");

        var a = new UserRelation { UserId = userId, FriendId = friendId, Status = RelationStatus.active_ };
        var b = new UserRelation { UserId = friendId, FriendId = userId, Status = RelationStatus.active_ };

        await _relations.CreateAsync(a, ct);
        await _relations.CreateAsync(b, ct);
        return true;
    }

    // ----------------------------------------------------------------------
    // UPDATE STATUS (block / inactive / active)
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid userId, Guid friendId, RelationStatus newStatus, CancellationToken ct)
    {
        var rel = await _relations.GetAsync(userId, friendId, ct);
        if (rel is null)
            throw new NotFoundException("Relation not found.");

        rel.Status = newStatus;
        return await _relations.UpdateAsync(rel, ct);
    }

    // ----------------------------------------------------------------------
    // DELETE RELATION (both sides)
    // ----------------------------------------------------------------------
    public async Task<bool> DeletePairAsync(Guid userId, Guid friendId, CancellationToken ct)
    {
        var success = await _relations.DeletePairAsync(userId, friendId, ct);
        if (!success)
            throw new NotFoundException("Relation not found.");
        return true;
    }

    // ----------------------------------------------------------------------
    // GET ALL ACTIVE RELATIONS
    // ----------------------------------------------------------------------
    public async Task<IEnumerable<UserRelationDto>> GetActiveAsync(Guid userId, CancellationToken ct)
    {
        var list = new List<UserRelationDto>();
        var rels = await _relations.GetFriendsAsync(userId, ct);

        foreach (var r in rels)
        {
            var f = await _users.GetByIdAsync(r.FriendId, ct);
            if (f is null) continue;

            list.Add(new UserRelationDto
            {
                UserId = r.UserId,
                FriendId = r.FriendId,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        return list;
    }

    // ----------------------------------------------------------------------
    // MUTUAL CHECK
    // ----------------------------------------------------------------------
    public async Task<bool> IsMutualAsync(Guid a, Guid b, CancellationToken ct)
    {
        var r1 = await _relations.GetAsync(a, b, ct);
        var r2 = await _relations.GetAsync(b, a, ct);
        return r1?.Status == RelationStatus.active_ && r2?.Status == RelationStatus.active_;
    }
}
