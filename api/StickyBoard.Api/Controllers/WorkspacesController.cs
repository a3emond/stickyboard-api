using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _service;

    public WorkspacesController(IWorkspaceService service)
    {
        _service = service;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<WorkspaceDto>>> Create(
        [FromBody] WorkspaceCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<WorkspaceDto>.Fail("Invalid or missing token."));

        var result = await _service.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<WorkspaceDto>.Ok(result));
    }

    // ------------------------------------------------------------
    // GET FOR CURRENT USER
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<WorkspaceDto>>>> GetForUser(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<WorkspaceDto>>.Fail("Invalid or missing token."));

        var result = await _service.GetForUserAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<WorkspaceDto>>.Ok(result));
    }

    // ------------------------------------------------------------
    // RENAME
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<ApiResponseDto<object>>> Rename(
        Guid id,
        [FromBody] WorkspaceRenameDto dto,
        CancellationToken ct)
    {
        await _service.RenameAsync(id, dto.Name, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // ADD MEMBER
    // ------------------------------------------------------------
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ApiResponseDto<object>>> AddMember(
        Guid id,
        [FromBody] WorkspaceAddMemberDto dto,
        CancellationToken ct)
    {
        await _service.AddMemberAsync(id, dto.UserId, dto.Role, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // REMOVE MEMBER
    // ------------------------------------------------------------
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        await _service.RemoveMemberAsync(id, userId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // CHANGE ROLE
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponseDto<object>>> ChangeRole(
        Guid id,
        Guid userId,
        [FromQuery] WorkspaceRole role,
        CancellationToken ct)
    {
        await _service.ChangeRoleAsync(id, userId, role, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // GET MEMBERS
    // ------------------------------------------------------------
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<WorkspaceMemberDto>>>> GetMembers(
        Guid id,
        CancellationToken ct)
    {
        var members = await _service.GetMembersAsync(id, ct);
        return Ok(ApiResponseDto<IEnumerable<WorkspaceMemberDto>>.Ok(members));
    }

    // ------------------------------------------------------------
    // GET USER ROLE (convenience)
    // ------------------------------------------------------------
    [HttpGet("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponseDto<WorkspaceRole>>> GetUserRole(
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        var role = await _service.GetUserRoleAsync(id, userId, ct);

        return role is null
            ? NotFound(ApiResponseDto<WorkspaceRole>.Fail("User is not a member of this workspace."))
            : Ok(ApiResponseDto<WorkspaceRole>.Ok(role.Value));
    }


    // ------------------------------------------------------------
    // DELETE (soft)
    // ------------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
