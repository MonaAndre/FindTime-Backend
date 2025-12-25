using FindTime.DTOs.AuthDTOs;
using FindTime.Services.Interfaces;
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
}