using StickyBoard.Api.DTOs.Organizations;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.Services
{
    public sealed class OrganizationService
    {
        private readonly OrganizationRepository _orgs;
        private readonly OrganizationMemberRepository _members;

        public OrganizationService(
            OrganizationRepository orgs,
            OrganizationMemberRepository members)
        {
            _orgs = orgs;
            _members = members;
        }

        private async Task EnsureOwnerAsync(Guid userId, Guid orgId, CancellationToken ct)
        {
            var org = await _orgs.GetByIdAsync(orgId, ct);
            if (org is null)
                throw new KeyNotFoundException("Organization not found.");

            if (org.OwnerId != userId)
                throw new UnauthorizedAccessException("Only the organization owner can modify this organization.");
        }

        // ----------------------------------------------------------------------
        // ORGANIZATION CRUD
        // ----------------------------------------------------------------------

        public async Task<Guid> CreateAsync(Guid ownerId, CreateOrganizationDto dto, CancellationToken ct)
        {
            var entity = new Organization
            {
                Name = dto.Name,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var id = await _orgs.CreateAsync(entity, ct);

            // Automatically add the owner as a member
            var member = new OrganizationMember
            {
                OrganizationId = id,
                UserId = ownerId,
                Role = OrgRole.owner
            };
            await _members.CreateAsync(member, ct);

            return id;
        }

        public async Task<bool> UpdateAsync(Guid actorId, Guid orgId, UpdateOrganizationDto dto, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, orgId, ct);
            var org = await _orgs.GetByIdAsync(orgId, ct);
            if (org is null)
                return false;

            org.Name = dto.Name ?? org.Name;
            org.UpdatedAt = DateTime.UtcNow;
            return await _orgs.UpdateAsync(org, ct);
        }

        public async Task<bool> DeleteAsync(Guid actorId, Guid orgId, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, orgId, ct);
            await _members.DeleteAsync(orgId, ct);
            return await _orgs.DeleteAsync(orgId, ct);
        }

        

        // ----------------------------------------------------------------------
        // MEMBERS
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<OrganizationMember>> GetMembersAsync(Guid userId, Guid orgId, CancellationToken ct)
        {
            await EnsureOwnerAsync(userId, orgId, ct);
            return await _members.GetByOrganizationAsync(orgId, ct);
        }

        public async Task<Guid> AddMemberAsync(Guid actorId, Guid orgId, AddMemberDto dto, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, orgId, ct);

            var entity = new OrganizationMember
            {
                OrganizationId = orgId,
                UserId = dto.UserId,
                Role = dto.Role
            };

            return await _members.CreateAsync(entity, ct);
        }

        public async Task<bool> UpdateMemberAsync(Guid actorId, Guid orgId, Guid memberId, UpdateMemberDto dto, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, orgId, ct);

            var member = new OrganizationMember
            {
                OrganizationId = orgId,
                UserId = memberId,
                Role = dto.Role
            };

            return await _members.UpdateAsync(member, ct);
        }

        public async Task<bool> RemoveMemberAsync(Guid actorId, Guid orgId, Guid memberId, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, orgId, ct);
            return await _members.RemoveMemberAsync(orgId, memberId, ct);
        }
    }
}
