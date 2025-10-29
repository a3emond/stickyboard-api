using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Files;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class FilesController : ControllerBase
    {
        private readonly FileService _files;

        public FilesController(FileService files)
        {
            _files = files;
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] CreateFileDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _files.CreateAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // GET (list by board)
        // ------------------------------------------------------------
        [HttpGet("board/{boardId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<FileDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<FileDto>>.Fail("Invalid or missing token."));

            var list = await _files.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<FileDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // GET (list by card)
        // ------------------------------------------------------------
        [HttpGet("card/{cardId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<FileDto>>>> GetByCard(Guid cardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<FileDto>>.Fail("Invalid or missing token."));

            var list = await _files.GetByCardAsync(userId, cardId, ct);
            return Ok(ApiResponseDto<IEnumerable<FileDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // GET (single file)
        // ------------------------------------------------------------
        [HttpGet("{fileId:guid}")]
        public async Task<ActionResult<ApiResponseDto<FileDto>>> Get(Guid fileId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<FileDto>.Fail("Invalid or missing token."));

            var file = await _files.GetAsync(userId, fileId, ct);
            return file is not null
                ? Ok(ApiResponseDto<FileDto>.Ok(file))
                : NotFound(ApiResponseDto<FileDto>.Fail("File not found."));
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{fileId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid fileId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var success = await _files.DeleteAsync(userId, fileId, ct);
            return success
                ? Ok(ApiResponseDto<object>.Ok(new { success = true, deleted = fileId }))
                : NotFound(ApiResponseDto<object>.Fail("File not found."));
        }
    }
}
