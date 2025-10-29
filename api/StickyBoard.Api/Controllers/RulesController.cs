using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Automation;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId:guid}/[controller]")]
    [Authorize]
    public sealed class RulesController : ControllerBase
    {
        private readonly RuleService _service;

        public RulesController(RuleService service)
        {
            _service = service;
        }

        // ------------------------------------------------------------
        // READ
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<RuleDto>>>> GetAll(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<RuleDto>>.Fail("Invalid or missing token."));

            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<RuleDto>>.Ok(items));
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Create(Guid boardId, [FromBody] CreateRuleDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        [HttpPut("{ruleId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid boardId, Guid ruleId, [FromBody] UpdateRuleDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(userId, ruleId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Rule not found."));
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{ruleId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid boardId, Guid ruleId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteAsync(userId, ruleId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Rule not found."));
        }
    }
}
