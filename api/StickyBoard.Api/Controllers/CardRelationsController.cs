using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Cards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/cards")]
    [Authorize]
    public sealed class CardRelationsController : ControllerBase
    {
        private readonly CardRelationsService _service;

        public CardRelationsController(CardRelationsService service)
        {
            _service = service;
        }

        // -------------------- TAGS --------------------

        [HttpGet("tags")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<TagDto>>>> GetTags([FromQuery] string? q, CancellationToken ct)
        {
            var tags = string.IsNullOrWhiteSpace(q)
                ? await _service.GetTagsAsync(ct)
                : await _service.SearchTagsAsync(q, ct);

            return Ok(ApiResponseDto<IEnumerable<TagDto>>.Ok(tags));
        }

        [HttpPost("tags")]
        public async Task<ActionResult<ApiResponseDto<object>>> CreateTag([FromBody] CreateTagDto dto, CancellationToken ct)
        {
            var id = await _service.CreateTagAsync(dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpGet("{cardId:guid}/tags")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<TagDto>>>> GetTagsForCard(Guid cardId, CancellationToken ct)
        {
            var tags = await _service.GetTagsForCardAsync(cardId, ct);
            return Ok(ApiResponseDto<IEnumerable<TagDto>>.Ok(tags));
        }

        [HttpPost("{cardId:guid}/tags")]
        public async Task<ActionResult<ApiResponseDto<object>>> AssignTags(Guid cardId, [FromBody] AssignTagDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            await _service.AssignTagsAsync(userId, cardId, dto.TagIds, ct);
            return Ok(ApiResponseDto<object>.Ok(new { success = true }));
        }

        [HttpDelete("{cardId:guid}/tags/{tagId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> RemoveTag(Guid cardId, Guid tagId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.RemoveTagAsync(userId, cardId, tagId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Tag not found."));
        }

        // -------------------- LINKS --------------------

        [HttpGet("{cardId:guid}/links/from")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<LinkDto>>>> GetLinksFrom(Guid cardId, CancellationToken ct)
        {
            var links = await _service.GetLinksFromAsync(cardId, ct);
            return Ok(ApiResponseDto<IEnumerable<LinkDto>>.Ok(links));
        }

        [HttpGet("{cardId:guid}/links/to")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<LinkDto>>>> GetLinksTo(Guid cardId, CancellationToken ct)
        {
            var links = await _service.GetLinksToAsync(cardId, ct);
            return Ok(ApiResponseDto<IEnumerable<LinkDto>>.Ok(links));
        }

        [HttpPost("{cardId:guid}/links")]
        public async Task<ActionResult<ApiResponseDto<object>>> CreateLink(Guid cardId, [FromBody] CreateLinkDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateLinkAsync(userId, cardId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("links/{linkId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateLink(Guid linkId, [FromBody] UpdateLinkDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateLinkAsync(userId, linkId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Link not found."));
        }

        [HttpDelete("links/{linkId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteLink(Guid linkId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteLinkAsync(userId, linkId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Link not found."));
        }
    }
}
