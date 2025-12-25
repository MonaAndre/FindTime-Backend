using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.AuthDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FindTime.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public async Task<ServiceResponse<AuthResponseDto>> RegisterAsync(RegisterDtoRequest dto)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResponse<AuthResponseDto>.ErrorResponse("A user with this email already exists",
                    400);
            }

            var newUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Birthday = dto.Birthday,
                RegisterDay = DateTime.UtcNow,
                IsConfirmed = true,
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResponse<AuthResponseDto>.ErrorResponse("Failed to create user", 400, errors);
            }

            var response = new AuthResponseDto
            {
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
            };

            return ServiceResponse<AuthResponseDto>.SuccessResponse(response, "User created", 200);
        }
        catch (Exception ex)
        {
            return ServiceResponse<AuthResponseDto>.ErrorResponse(
                $"Registration failed: {ex.Message}",
                500);
        }
    }

    public async Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginDtoRequest dto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return ServiceResponse<AuthResponseDto>.ErrorResponse(
                    "Invalid email or password",
                    401
                );
            if (user.IsDeleted)
            {
                return ServiceResponse<AuthResponseDto>.ErrorResponse(
                    "This account has been deleted",
                    403
                );
            }

            var result = await _signInManager.PasswordSignInAsync(user, dto.Password, true, false);
            if (!result.Succeeded)
                return ServiceResponse<AuthResponseDto>.ErrorResponse("Invalid email or password ", 401);

            var response = new AuthResponseDto
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            return ServiceResponse<AuthResponseDto>.SuccessResponse(
                response,
                "Login successful"
            );
        }
        catch (Exception ex)
        {
            return ServiceResponse<AuthResponseDto>.ErrorResponse(
                $"Registration failed: {ex.Message}",
                500);
        }
    }

    public async Task<ServiceResponse<bool>> LogoutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse(e.Message, statusCode: 500);
        }
    }
}