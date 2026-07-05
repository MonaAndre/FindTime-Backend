using System.Security.Claims;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActivityController(IActivityService activityService) : ControllerBase
{
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpGet("get-group-activity/{groupId}")]
    public async Task<IActionResult> GetGroupActivity(int groupId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await activityService.GetGroupActivityAsync(groupId, userId, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-user-activity")]
    public async Task<IActionResult> GetUserActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await activityService.GetUserActivityAsync(userId, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }
}
