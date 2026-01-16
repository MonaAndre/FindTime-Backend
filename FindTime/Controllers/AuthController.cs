using System.Security.Claims;
using FindTime.Common;
using FindTime.DTOs.AuthDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager)
    {
        _authService = authService;
        _userManager = userManager;
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

    [HttpGet("me")]
    public async Task<ServiceResponse<AuthResponseDto>> GetCurrentUser()
    {
        try
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return ServiceResponse<AuthResponseDto>.UnauthorizedResponse("User not authenticated");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse<AuthResponseDto>.UnauthorizedResponse("User ID not found in claims");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<AuthResponseDto>.UnauthorizedResponse("User not found");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return ServiceResponse<AuthResponseDto>.UnauthorizedResponse("User account is locked");
            }

            var userDto = new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            return ServiceResponse<AuthResponseDto>.SuccessResponse(userDto, "User authenticated");
        }
        catch (Exception ex)
        {
            return ServiceResponse<AuthResponseDto>.ErrorResponse($"Error retrieving user: {ex.Message}");
        }
    }
}