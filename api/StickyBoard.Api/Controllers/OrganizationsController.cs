using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _service;

    public OrganizationsController(OrganizationService service)
    {
        _service = service;
    }

    // ------------------------------------------------------------
    // GET: /api/organizations
    // List all orgs the user belongs to (owned + member)
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<OrganizationDto>>>> List(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<OrganizationDto>>.Fail("Invalid token"));

        var orgs = await _service.GetMyOrganizationsAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<OrganizationDto>>.Ok(orgs));
    }

    // ------------------------------------------------------------
    // GET: /api/organizations/owned
    // List orgs owned by user
    // ------------------------------------------------------------
    [HttpGet("owned")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<OrganizationDto>>>> Owned(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<OrganizationDto>>.Fail("Invalid token"));

        var orgs = await _service.GetOwnedAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<OrganizationDto>>.Ok(orgs));
    }

    // ------------------------------------------------------------
    // POST: /api/organizations
    // Create organization
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] OrganizationCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        var id = await _service.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    // ------------------------------------------------------------
    // PUT: /api/organizations/{orgId}
    // Rename organization (owner only)
    // ------------------------------------------------------------
    [HttpPut("{orgId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid orgId, [FromBody] OrganizationUpdateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        var ok = await _service.UpdateAsync(userId, orgId, dto, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Organization not found"));
    }

    // ------------------------------------------------------------
    // DELETE: /api/organizations/{orgId}
    // Owner only (soft delete)
    // ------------------------------------------------------------
    [HttpDelete("{orgId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid orgId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        var ok = await _service.DeleteAsync(userId, orgId, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Organization not found"));
    }

    // ------------------------------------------------------------
    // GET: /api/organizations/{orgId}/members
    // List members
    // ------------------------------------------------------------
    [HttpGet("{orgId:guid}/members")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<OrganizationMemberDto>>>> Members(Guid orgId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<OrganizationMemberDto>>.Fail("Invalid token"));

        var members = await _service.GetMembersAsync(userId, orgId, ct);
        return Ok(ApiResponseDto<IEnumerable<OrganizationMemberDto>>.Ok(members));
    }

    // ------------------------------------------------------------
    // POST: /api/organizations/{orgId}/members/{userId}
    // Add member
    // ------------------------------------------------------------
    [HttpPost("{orgId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> AddMember(Guid orgId, Guid userId, [FromQuery] OrgRole role, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        await _service.AddMemberAsync(actorId, orgId, userId, role, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // PUT: /api/organizations/{orgId}/members/{userId}
    // Change role
    // ------------------------------------------------------------
    [HttpPut("{orgId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateMember(Guid orgId, Guid userId, [FromQuery] OrgRole role, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        await _service.UpdateMemberRoleAsync(actorId, orgId, userId, role, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // DELETE: /api/organizations/{orgId}/members/{userId}
    // Remove member
    // ------------------------------------------------------------
    [HttpDelete("{orgId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> RemoveMember(Guid orgId, Guid userId, CancellationToken ct)
    {
        var actorId = User.GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid token"));

        await _service.RemoveMemberAsync(actorId, orgId, userId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
