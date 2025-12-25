using System.Security.Claims;
using FindTime.DTOs.UserDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpGet("get-user")]
    public async Task<IActionResult> GetUser()
    {
        var userId = GetUserId();
        var result = await _userService.GetUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("update-user")]
    public async Task<IActionResult> UpdateUserAsync([FromBody] UserDto dto)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateUserAsync(userId, dto);
        return StatusCode(result.StatusCode, result);
    }
}