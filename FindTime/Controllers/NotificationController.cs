using System.Security.Claims;
using FindTime.DTOs.NotificationDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpGet("get-notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await notificationService.GetNotificationsAsync(userId, unreadOnly, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var result = await notificationService.GetUnreadCountAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("mark-as-read")]
    public async Task<IActionResult> MarkAsRead(MarkNotificationAsReadDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await notificationService.MarkAsReadAsync(dto.NotificationId, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        var result = await notificationService.MarkAllAsReadAsync(userId);
        return StatusCode(result.StatusCode, result);
    }
}
