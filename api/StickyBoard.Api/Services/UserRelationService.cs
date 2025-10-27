using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Social;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.DTOs.Users;

namespace StickyBoard.Api.Services
{
    public sealed class UserRelationService
    {
        private readonly UserRelationRepository _relations;
        private readonly UserRepository _users;

        public UserRelationService(UserRelationRepository relations, UserRepository users)
        {
            _relations = relations;
            _users = users;
        }

        // ------------------------------------------------------------
        // CREATE (add relation both ways)
        // ------------------------------------------------------------
        public async Task<bool> CreateAsync(Guid userId, Guid friendId, CancellationToken ct)
        {
            var a = new UserRelation { UserId = userId, FriendId = friendId, Status = RelationStatus.active };
            var b = new UserRelation { UserId = friendId, FriendId = userId, Status = RelationStatus.active };
            await _relations.CreateAsync(a, ct);
            await _relations.CreateAsync(b, ct);
            return true;
        }

        // ------------------------------------------------------------
        // UPDATE (block, inactive, etc.)
        // ------------------------------------------------------------
        public async Task<bool> UpdateAsync(Guid userId, Guid friendId, RelationStatus status, CancellationToken ct)
        {
            var relation = new UserRelation
            {
                UserId = userId,
                FriendId = friendId,
                Status = status
            };
            return await _relations.UpdateAsync(relation, ct);
        }

        // ------------------------------------------------------------
        // REMOVE BOTH DIRECTIONS
        // ------------------------------------------------------------
        public async Task<bool> DeletePairAsync(Guid userId, Guid friendId, CancellationToken ct)
            => await _relations.DeletePairAsync(userId, friendId, ct);

        // ------------------------------------------------------------
        // GET ALL ACTIVE RELATIONS
        // ------------------------------------------------------------
        public async Task<IEnumerable<UserRelationDto>> GetActiveAsync(Guid userId, CancellationToken ct)
        {
            var relations = await _relations.GetFriendsAsync(userId, ct);
            var list = new List<UserRelationDto>();

            foreach (var rel in relations)
            {
                var friend = await _users.GetByIdAsync(rel.FriendId, ct);
                list.Add(new UserRelationDto
                {
                    FriendId = rel.FriendId,
                    DisplayName = friend?.DisplayName,
                    Email = friend?.Email,
                    Status = rel.Status.ToString(),
                    CreatedAt = rel.CreatedAt,
                    UpdatedAt = rel.UpdatedAt
                });
            }

            return list;
        }

        // ------------------------------------------------------------
        // MUTUAL CHECK
        // ------------------------------------------------------------
        public async Task<bool> IsMutualAsync(Guid a, Guid b, CancellationToken ct)
        {
            var r1 = await _relations.GetAsync(a, b, ct);
            var r2 = await _relations.GetAsync(b, a, ct);
            return r1?.Status == RelationStatus.active && r2?.Status == RelationStatus.active;
        }
    }
}
