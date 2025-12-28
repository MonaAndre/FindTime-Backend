using System.Security.Claims;
using FindTime.DTOs.AuthDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDtoRequest dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDtoRequest dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync();
        return StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _authService.ChangePasswordAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }
    
}