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
    public sealed class ClustersController : ControllerBase
    {
        private readonly ClusterService _service;

        public ClustersController(ClusterService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var clusters = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(clusters);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateClusterDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{clusterId:guid}")]
        public async Task<IActionResult> Update(Guid boardId, Guid clusterId, [FromBody] UpdateClusterDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _service.UpdateAsync(userId, clusterId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{clusterId:guid}")]
        public async Task<IActionResult> Delete(Guid boardId, Guid clusterId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _service.DeleteAsync(userId, clusterId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}