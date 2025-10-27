using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Organizations;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        // ----------------------------------------------------------------------
        // ORGANIZATIONS
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateAsync(userId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{orgId:guid}")]
        public async Task<IActionResult> Update(Guid orgId, [FromBody] UpdateOrganizationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateAsync(userId, orgId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{orgId:guid}")]
        public async Task<IActionResult> Delete(Guid orgId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.DeleteAsync(userId, orgId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        // ----------------------------------------------------------------------
        // MEMBERS
        // ----------------------------------------------------------------------

        [HttpGet("{orgId:guid}/members")]
        public async Task<IActionResult> GetMembers(Guid orgId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var members = await _service.GetMembersAsync(userId, orgId, ct);
            return Ok(members);
        }

        [HttpPost("{orgId:guid}/members")]
        public async Task<IActionResult> AddMember(Guid orgId, [FromBody] AddMemberDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.AddMemberAsync(userId, orgId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{orgId:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> UpdateMember(Guid orgId, Guid memberId, [FromBody] UpdateMemberDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateMemberAsync(userId, orgId, memberId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{orgId:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid orgId, Guid memberId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.RemoveMemberAsync(userId, orgId, memberId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
