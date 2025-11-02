using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.Organizations;
using StickyBoard.Api.Repositories.Organizations;

namespace StickyBoard.Api.Services;

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

    // ------------------------------------------------------------
    // CREATE ORG (Owner only) + add owner as member
    // ------------------------------------------------------------
    public async Task<Guid> CreateAsync(Guid ownerId, OrganizationCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Organization name is required.");

        var org = new Organization
        {
            OwnerId = ownerId,
            Name = dto.Name.Trim()
        };

        var orgId = await _orgs.CreateAsync(org, ct);

        // Add owner to membership table
        await _members.CreateAsync(new OrganizationMember
        {
            OrgId = orgId,
            UserId = ownerId,
            Role = OrgRole.owner
        }, ct);

        return orgId;
    }

    // ------------------------------------------------------------
    // UPDATE ORG NAME (Owner only)
    // ------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid actorId, Guid orgId, OrganizationUpdateDto dto, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(orgId, ct)
                  ?? throw new NotFoundException("Organization not found.");

        if (org.OwnerId != actorId)
            throw new ForbiddenException("Only the owner can rename organization.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Organization name cannot be empty.");

        org.Name = dto.Name.Trim();

        return await _orgs.UpdateAsync(org, ct);
    }

    // ------------------------------------------------------------
    // DELETE ORG (Owner only)
    // ------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid actorId, Guid orgId, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(orgId, ct)
                  ?? throw new NotFoundException("Organization not found.");

        if (org.OwnerId != actorId)
            throw new ForbiddenException("Only the owner can delete organization.");

        // soft delete handled by base
        return await _orgs.DeleteAsync(orgId, ct);
    }

    // ------------------------------------------------------------
    // LIST orgs owned by user
    // ------------------------------------------------------------
    public async Task<IEnumerable<OrganizationDto>> GetOwnedAsync(Guid actorId, CancellationToken ct)
    {
        var list = await _orgs.GetByOwnerAsync(actorId, ct);

        return list.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            OwnerId = o.OwnerId
        });
    }

    // ------------------------------------------------------------
    // LIST orgs user belongs to (including owned)
    // ------------------------------------------------------------
    public async Task<IEnumerable<OrganizationDto>> GetMyOrganizationsAsync(Guid userId, CancellationToken ct)
    {
        var list = await _orgs.GetForUserAsync(userId, ct);

        return list.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            OwnerId = o.OwnerId
        });
    }

    // ------------------------------------------------------------
    // LIST members
    // ------------------------------------------------------------
    public async Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid actorId, Guid orgId, CancellationToken ct)
    {
        await EnsureCanViewMembers(actorId, orgId, ct);

        var members = await _members.GetByOrganizationAsync(orgId, ct);

        return members.Select(m => new OrganizationMemberDto
        {
            User = new UserDto { Id = m.UserId }, // hydrate later
            Role = m.Role
        });
    }

    // ------------------------------------------------------------
    // ADD member (owner + admin)
    // ------------------------------------------------------------
    public async Task AddMemberAsync(Guid actorId, Guid orgId, Guid userId, OrgRole role, CancellationToken ct)
    {
        await EnsureCanManageMembers(actorId, orgId, ct);

        if (await _members.ExistsAsync(orgId, userId, ct))
            throw new ValidationException("User already in organization.");

        await _members.CreateAsync(new OrganizationMember
        {
            OrgId = orgId,
            UserId = userId,
            Role = role
        }, ct);
    }

    // ------------------------------------------------------------
    // UPDATE member role (owner + admin)
    // ------------------------------------------------------------
    public async Task UpdateMemberRoleAsync(Guid actorId, Guid orgId, Guid userId, OrgRole role, CancellationToken ct)
    {
        await EnsureCanManageMembers(actorId, orgId, ct);

        var existing = await _members.GetMemberAsync(orgId, userId, ct)
                       ?? throw new NotFoundException("User is not a member of organization.");

        existing.Role = role;

        await _members.UpdateAsync(existing, ct);
    }

    // ------------------------------------------------------------
    // REMOVE member (owner + admin)
    // ------------------------------------------------------------
    public async Task RemoveMemberAsync(Guid actorId, Guid orgId, Guid userId, CancellationToken ct)
    {
        await EnsureCanManageMembers(actorId, orgId, ct);

        await _members.RemoveMemberAsync(orgId, userId, ct);
    }

    // ------------------------------------------------------------
    // Permission helpers
    // ------------------------------------------------------------
    private async Task EnsureCanViewMembers(Guid actorId, Guid orgId, CancellationToken ct)
    {
        var member = await _members.GetMemberAsync(orgId, actorId, ct);

        if (member == null)
            throw new ForbiddenException("Not part of organization.");
    }

    private async Task EnsureCanManageMembers(Guid actorId, Guid orgId, CancellationToken ct)
    {
        var member = await _members.GetMemberAsync(orgId, actorId, ct);

        if (member == null)
            throw new ForbiddenException("Not part of organization.");

        if (member.Role is not (OrgRole.owner or OrgRole.admin))
            throw new ForbiddenException("Only owner or admin can manage members.");
    }
}
