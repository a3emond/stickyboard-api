using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BoardsController : ControllerBase
{
    private readonly IBoardService _boards;

    public BoardsController(IBoardService boards)
    {
        _boards = boards;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost("{workspaceId:guid}")]
    public async Task<ActionResult<ApiResponseDto<BoardDto>>> Create(
        Guid workspaceId,
        [FromBody] BoardCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<BoardDto>.Fail("Invalid or missing token."));

        var board = await _boards.CreateAsync(workspaceId, userId, dto, ct);

        return Ok(ApiResponseDto<BoardDto>.Ok(board));
    }

    // ------------------------------------------------------------
    // GET FOR WORKSPACE
    // ------------------------------------------------------------
    [HttpGet("workspace/{workspaceId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<BoardDto>>>> GetForWorkspace(
        Guid workspaceId,
        CancellationToken ct)
    {
        var list = await _boards.GetForWorkspaceAsync(workspaceId, ct);

        return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // RENAME
    // ------------------------------------------------------------
    [HttpPut("{boardId:guid}/rename")]
    public async Task<ActionResult<ApiResponseDto<object>>> Rename(
        Guid boardId,
        [FromBody] BoardRenameDto dto,
        CancellationToken ct)
    {
        await _boards.RenameAsync(boardId, dto.Title, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [HttpDelete("{boardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(
        Guid boardId,
        CancellationToken ct)
    {
        await _boards.DeleteAsync(boardId, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // ADD MEMBER (override)
    // ------------------------------------------------------------
    [HttpPost("{boardId:guid}/members")]
    public async Task<ActionResult<ApiResponseDto<object>>> AddMember(
        Guid boardId,
        [FromBody] BoardAddMemberDto dto,
        CancellationToken ct)
    {
        await _boards.AddMemberAsync(boardId, dto.UserId, dto.Role, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // REMOVE MEMBER
    // ------------------------------------------------------------
    [HttpDelete("{boardId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> RemoveMember(
        Guid boardId,
        Guid userId,
        CancellationToken ct)
    {
        await _boards.RemoveMemberAsync(boardId, userId, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // CHANGE MEMBER ROLE
    // ------------------------------------------------------------
    [HttpPut("{boardId:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponseDto<object>>> ChangeRole(
        Guid boardId,
        Guid userId,
        [FromQuery] WorkspaceRole role,
        CancellationToken ct)
    {
        await _boards.ChangeRoleAsync(boardId, userId, role, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // GET MEMBERS
    // ------------------------------------------------------------
    [HttpGet("{boardId:guid}/members")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<BoardMemberDto>>>> GetMembers(
        Guid boardId,
        CancellationToken ct)
    {
        var list = await _boards.GetMembersAsync(boardId, ct);

        return Ok(ApiResponseDto<IEnumerable<BoardMemberDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // GET USER ROLE (override check)
    // ------------------------------------------------------------
    [HttpGet("{boardId:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponseDto<WorkspaceRole?>>> GetUserRole(
        Guid boardId,
        Guid userId,
        CancellationToken ct)
    {
        var role = await _boards.GetUserRoleAsync(boardId, userId, ct);

        return Ok(ApiResponseDto<WorkspaceRole?>.Ok(role));
    }
}
