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
public sealed class UserRelationsController : ControllerBase
{
    private readonly UserRelationService _relations;

    public UserRelationsController(UserRelationService relations)
    {
        _relations = relations;
    }

    // ------------------------------------------------------------
    // GET ALL RELATIONS (active only)
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<UserRelationDto>>>> GetRelations(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<UserRelationDto>>.Fail("Invalid or missing token."));

        var list = await _relations.GetActiveAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<UserRelationDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // CREATE RELATION
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<object>>> CreateRelation([FromBody] UserRelationCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        await _relations.CreateAsync(userId, dto.FriendId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // UPDATE RELATION STATUS
    // ------------------------------------------------------------
    [HttpPut("{friendId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateRelation(
        Guid friendId,
        [FromBody] UserRelationUpdateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _relations.UpdateAsync(userId, friendId, dto.Status, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Relation not found or not allowed."));
    }


    // ------------------------------------------------------------
    // DELETE RELATION
    // ------------------------------------------------------------
    [HttpDelete("{friendId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> DeleteRelation(Guid friendId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        await _relations.DeletePairAsync(userId, friendId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
