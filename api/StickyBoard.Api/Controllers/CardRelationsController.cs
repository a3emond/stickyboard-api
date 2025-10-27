using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Cards;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task<IActionResult> GetTags([FromQuery] string? q, CancellationToken ct)
        {
            var tags = string.IsNullOrWhiteSpace(q)
                ? await _service.GetTagsAsync(ct)
                : await _service.SearchTagsAsync(q, ct);
            return Ok(tags);
        }

        [HttpPost("tags")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto, CancellationToken ct)
        {
            var id = await _service.CreateTagAsync(dto, ct);
            return Ok(new { id });
        }

        [HttpGet("{cardId:guid}/tags")]
        public async Task<IActionResult> GetTagsForCard(Guid cardId, CancellationToken ct)
        {
            var tags = await _service.GetTagsForCardAsync(cardId, ct);
            return Ok(tags);
        }

        [HttpPost("{cardId:guid}/tags")]
        public async Task<IActionResult> AssignTags(Guid cardId, [FromBody] AssignTagDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _service.AssignTagsAsync(userId, cardId, dto.TagIds, ct);
            return Ok(new { success = true });
        }

        [HttpDelete("{cardId:guid}/tags/{tagId:guid}")]
        public async Task<IActionResult> RemoveTag(Guid cardId, Guid tagId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.RemoveTagAsync(userId, cardId, tagId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        // -------------------- LINKS --------------------

        [HttpGet("{cardId:guid}/links/from")]
        public async Task<IActionResult> GetLinksFrom(Guid cardId, CancellationToken ct)
        {
            var links = await _service.GetLinksFromAsync(cardId, ct);
            return Ok(links);
        }

        [HttpGet("{cardId:guid}/links/to")]
        public async Task<IActionResult> GetLinksTo(Guid cardId, CancellationToken ct)
        {
            var links = await _service.GetLinksToAsync(cardId, ct);
            return Ok(links);
        }

        [HttpPost("{cardId:guid}/links")]
        public async Task<IActionResult> CreateLink(Guid cardId, [FromBody] CreateLinkDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateLinkAsync(userId, cardId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("links/{linkId:guid}")]
        public async Task<IActionResult> UpdateLink(Guid linkId, [FromBody] UpdateLinkDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateLinkAsync(userId, linkId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("links/{linkId:guid}")]
        public async Task<IActionResult> DeleteLink(Guid linkId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.DeleteLinkAsync(userId, linkId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
