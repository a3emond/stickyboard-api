using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Services.SocialAndMessaging;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications)
    {
        _notifications = notifications;
    }

    // ------------------------------------------------------------
    // GET MY NOTIFICATIONS
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<NotificationDto>>>> GetMyNotifications(
        [FromQuery] bool unreadOnly,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<NotificationDto>>.Fail("Invalid or missing token."));

        var list = await _notifications.GetForUserAsync(userId, unreadOnly, ct);
        return Ok(ApiResponseDto<IEnumerable<NotificationDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // MARK SINGLE AS READ
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponseDto<object>>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _notifications.MarkAsReadAsync(id, userId, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Notification not found or not owned by user."));
    }

    // ------------------------------------------------------------
    // MARK ALL AS READ
    // ------------------------------------------------------------
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponseDto<object>>> MarkAllAsRead(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var count = await _notifications.MarkAllAsReadAsync(userId, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true, updated = count }));
    }
}
