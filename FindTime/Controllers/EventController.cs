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
    private readonly IEventService _eventService = eventService;

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
}