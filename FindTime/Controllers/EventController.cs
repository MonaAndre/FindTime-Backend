using System.Security.Claims;
using FindTime.DTOs.EventDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventController(IEventService eventService) : ControllerBase
{

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpPost("create-event")]
    public async Task<IActionResult> CreateEvent(CreateEventDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await eventService.CreateEventAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("update-event")]
    public async Task<IActionResult> UpdateEvent(UpdateEventDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await eventService.UpdateEventAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("delete-event")]
    public async Task<IActionResult> DeleteEvent(DeleteEventDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await eventService.DeleteEventAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("get-group-events/{groupId}")]
    public async Task<IActionResult> GetAllGroupEvents(int groupId)
    {
        var userId = GetUserId();
        var result = await eventService.GetAllGroupEventsAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }
}