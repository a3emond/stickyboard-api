using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Organizations;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class OrganizationsController : ControllerBase
    {
        private readonly OrganizationService _service;

        public OrganizationsController(OrganizationService service)
        {
            _service = service;
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] CreateOrganizationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        [HttpPut("{orgId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid orgId, [FromBody] UpdateOrganizationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(userId, orgId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Organization not found."));
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{orgId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid orgId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteAsync(userId, orgId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Organization not found."));
        }

        // ------------------------------------------------------------
        // MEMBERS
        // ------------------------------------------------------------
        [HttpGet("{orgId:guid}/members")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<OrganizationMemberDto>>>> GetMembers(Guid orgId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<OrganizationMemberDto>>.Fail("Invalid or missing token."));

            var members = await _service.GetMembersAsync(userId, orgId, ct);
            return Ok(ApiResponseDto<IEnumerable<OrganizationMemberDto>>.Ok(members));
        }

        [HttpPost("{orgId:guid}/members")]
        public async Task<ActionResult<ApiResponseDto<object>>> AddMember(Guid orgId, [FromBody] AddMemberDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.AddMemberAsync(userId, orgId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("{orgId:guid}/members/{memberId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateMember(Guid orgId, Guid memberId, [FromBody] UpdateMemberDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateMemberAsync(userId, orgId, memberId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Member not found."));
        }

        [HttpDelete("{orgId:guid}/members/{memberId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> RemoveMember(Guid orgId, Guid memberId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.RemoveMemberAsync(userId, orgId, memberId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Member not found."));
        }
    }
}
