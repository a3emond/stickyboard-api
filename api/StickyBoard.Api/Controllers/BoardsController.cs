using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Boards;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;


namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class BoardsController : ControllerBase
    {
        private readonly BoardService _service;

        public BoardsController(BoardService service)
        {
            _service = service;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> Mine(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var boards = await _service.GetMineAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(boards));
        }

        [HttpGet("accessible")]
        public async Task<IActionResult> Accessible(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var boards = await _service.GetAccessibleAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(boards));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBoardDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBoardDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
            return ok ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Board not found."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Board not found."));
        }
    }
}
