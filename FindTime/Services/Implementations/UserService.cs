using System.Text.RegularExpressions;
using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.UserDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FindTime.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public UserService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public async Task<ServiceResponse<UserDto>> GetUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return ServiceResponse<UserDto>.NotFoundResponse("User not found");
            }

            var foundUser = new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Birthday = user.Birthday,
                PhoneNumber = user.PhoneNumber,
                ProfilePicLink = user.ProfilePicLink
            };
            return ServiceResponse<UserDto>.SuccessResponse(foundUser);
        }
        catch (Exception e)
        {
            return ServiceResponse<UserDto>.ErrorResponse(e.Message, 500);
        }
    }

    public async Task<ServiceResponse<bool>> UpdateUserAsync(string userId, UserDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return ServiceResponse<bool>.NotFoundResponse("User not found");
            }

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResponse<bool>.ErrorResponse("Email already exists");
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
            if (string.IsNullOrWhiteSpace(dto.Email) || !Regex.IsMatch(dto.Email, emailPattern))
            {
                return ServiceResponse<bool>.ErrorResponse("Email can not be empty and need to be valid");
            }

            user.Email = dto.Email;
            if (string.IsNullOrWhiteSpace(dto.FirstName))
            {
                return ServiceResponse<bool>.ErrorResponse("First name can not be empty");
            }

            user.FirstName = dto.FirstName;
            if (string.IsNullOrWhiteSpace(dto.LastName))
            {
                return ServiceResponse<bool>.ErrorResponse("Last name can not be empty");
            }

            user.LastName = dto.LastName;
            if (!dto.Birthday.HasValue)
            {
                return ServiceResponse<bool>.ErrorResponse("Birthday can not be empty");
            }

            user.Birthday = dto.Birthday;
            if (string.IsNullOrWhiteSpace(dto.ProfilePicLink))
            {
                return ServiceResponse<bool>.ErrorResponse("Picture link can not be empty");
            }

            user.ProfilePicLink = dto.ProfilePicLink;
            string phonePattern = @"^\+?\d[\d\s\-]{6,14}\d$";
            if (!Regex.IsMatch(dto.PhoneNumber, phonePattern))
            {
                return ServiceResponse<bool>.ErrorResponse("Phone can not be empty and need to be valid");
            }

            user.PhoneNumber = dto.PhoneNumber;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResponse<bool>.ErrorResponse(result.Errors.First().Description, 500);
            }

            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to update users information. Error message: {e}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return ServiceResponse<bool>.NotFoundResponse("User not found");
            }
            
            user.IsDeleted = true;
            user.Email = $"{Guid.NewGuid()}@DELETED.Com";
            user.NormalizedEmail = user.Email.ToUpper();
            user.UserName = $"deleted_{Guid.NewGuid():N}";
            user.NormalizedUserName = user.UserName.ToUpperInvariant();
            user.PhoneNumber = null;
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.DeletedAt = DateTime.UtcNow;
            user.IsConfirmed = false;
            var result = await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync();
            if (!result.Succeeded)
            {
                return ServiceResponse<bool>.ErrorResponse(result.Errors.First().Description, 500);
            }
            return ServiceResponse<bool>.SuccessResponse(result.Succeeded);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to delete user", 500);
        }
    }
}