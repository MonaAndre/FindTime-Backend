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

    [HttpGet("get-group-events/{groupId}")]
    public async Task<IActionResult> GetAllGroupEvents(int groupId, [FromQuery] DateTime rangeStart, [FromQuery] DateTime rangeEnd)
    {
        var userId = GetUserId();
        var result = await eventService.GetAllGroupEventsAsync(groupId, userId, rangeStart, rangeEnd);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("get-next-event/{groupId}")]
    public async Task<IActionResult> GetNextEvent(int groupId)
    {
        var userId = GetUserId();
        var result = await eventService.GetNextEvent(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }
    [HttpGet("get-all-events-next-week")]
    public async Task<IActionResult> GetAllEventsNextWeek()
    {
        var userId = GetUserId();
        var result = await eventService.GetAllEventsNextWeekAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("find-free-slot/{groupId}")]
    public async Task<IActionResult> FindFreeSlot(int groupId, [FromQuery] int durationMinutes = 60,
        [FromQuery] int lookAheadDays = 14)
    {
        var userId = GetUserId();
        var result = await eventService.FindFreeSlotAsync(groupId, userId, durationMinutes, lookAheadDays);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("respond-to-event")]
    public async Task<IActionResult> RespondToEvent(RespondToEventDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await eventService.RespondToEventAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-event-participants/{eventId}")]
    public async Task<IActionResult> GetEventParticipants(int eventId)
    {
        var userId = GetUserId();
        var result = await eventService.GetEventParticipantsAsync(eventId, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-event/{groupId}/{eventId}")]
    public async Task<IActionResult> GetEvent(int groupId, int eventId)
    {
        var userId = GetUserId();
        var result = await eventService.GetEventAsync(groupId, eventId, userId);
        return StatusCode(result.StatusCode, result);
    }
}