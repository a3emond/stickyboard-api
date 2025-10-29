using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Files;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all endpoints require valid JWT
    public sealed class FilesController : ControllerBase
    {
        private readonly FileService _files;

        public FilesController(FileService files)
        {
            _files = files;
        }

        // ------------------------------------------------------------
        // CREATE (upload metadata)
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFileDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _files.CreateAsync(userId, dto, ct);
            return Ok(new { Id = id });
        }

        // ------------------------------------------------------------
        // GET (list by board)
        // ------------------------------------------------------------
        [HttpGet("board/{boardId:guid}")]
        public async Task<IActionResult> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var list = await _files.GetByBoardAsync(userId, boardId, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // GET (list by card)
        // ------------------------------------------------------------
        [HttpGet("card/{cardId:guid}")]
        public async Task<IActionResult> GetByCard(Guid cardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var list = await _files.GetByCardAsync(userId, cardId, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // GET (single file)
        // ------------------------------------------------------------
        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> Get(Guid fileId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var file = await _files.GetAsync(userId, fileId, ct);
            return file is not null ? Ok(file) : NotFound();
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> Delete(Guid fileId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _files.DeleteAsync(userId, fileId, ct);
            return success
                ? Ok(new { success = true, deleted = fileId })
                : NotFound();
        }
    }
}
