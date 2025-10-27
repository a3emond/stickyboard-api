using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Automation;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

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

        [HttpGet]
        public async Task<IActionResult> GetAll(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateRuleDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{ruleId:guid}")]
        public async Task<IActionResult> Update(Guid boardId, Guid ruleId, [FromBody] UpdateRuleDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _service.UpdateAsync(userId, ruleId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{ruleId:guid}")]
        public async Task<IActionResult> Delete(Guid boardId, Guid ruleId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _service.DeleteAsync(userId, ruleId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}