using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Users;

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
        public async Task<IActionResult> GetRelations(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var list = await _relations.GetActiveAsync(userId, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // POST /api/userrelations
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateRelation([FromBody] UserRelationCreateDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _relations.CreateAsync(userId, dto.FriendId, ct);
            return success ? Ok(new { success = true }) : BadRequest("Could not create relation.");
        }

        // ------------------------------------------------------------
        // PUT /api/userrelations/{userId}/{friendId}
        // ------------------------------------------------------------
        [HttpPut("{userId:guid}/{friendId:guid}")]
        public async Task<IActionResult> UpdateRelation(Guid userId, Guid friendId, [FromBody] UserRelationUpdateDto dto, CancellationToken ct)
        {
            var callerId = User.GetUserId();
            if (callerId != userId)
                return Forbid();

            if (!Enum.TryParse(dto.Status, true, out RelationStatus newStatus))
                return BadRequest("Invalid relation status.");

            var success = await _relations.UpdateAsync(userId, friendId, newStatus, ct);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // ------------------------------------------------------------
        // DELETE /api/userrelations/{userId}/{friendId}
        // ------------------------------------------------------------
        [HttpDelete("{userId:guid}/{friendId:guid}")]
        public async Task<IActionResult> DeleteRelation(Guid userId, Guid friendId, CancellationToken ct)
        {
            var callerId = User.GetUserId();
            if (callerId != userId)
                return Forbid();

            var success = await _relations.DeletePairAsync(userId, friendId, ct);
            return success ? Ok(new { success = true }) : NotFound();
        }
    }
}
