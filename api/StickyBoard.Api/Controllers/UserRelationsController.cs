using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Users;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
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
        // GET /api/userrelations
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
        // POST /api/userrelations
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> CreateRelation([FromBody] UserRelationCreateDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var success = await _relations.CreateAsync(userId, dto.FriendId, ct);
            return success
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : BadRequest(ApiResponseDto<object>.Fail("Could not create relation."));
        }

        // ------------------------------------------------------------
        // PUT /api/userrelations/{userId}/{friendId}
        // ------------------------------------------------------------
        [HttpPut("{userId:guid}/{friendId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateRelation(Guid userId, Guid friendId, [FromBody] UserRelationUpdateDto dto, CancellationToken ct)
        {
            var callerId = User.GetUserId();
            if (callerId != userId)
                return Forbid();

            if (!Enum.TryParse(dto.Status, true, out RelationStatus newStatus))
                return BadRequest(ApiResponseDto<object>.Fail("Invalid relation status."));

            var success = await _relations.UpdateAsync(userId, friendId, newStatus, ct);
            return success
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Relation not found."));
        }

        // ------------------------------------------------------------
        // DELETE /api/userrelations/{userId}/{friendId}
        // ------------------------------------------------------------
        [HttpDelete("{userId:guid}/{friendId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteRelation(Guid userId, Guid friendId, CancellationToken ct)
        {
            var callerId = User.GetUserId();
            if (callerId != userId)
                return Forbid();

            var success = await _relations.DeletePairAsync(userId, friendId, ct);
            return success
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Relation not found."));
        }
    }
}
