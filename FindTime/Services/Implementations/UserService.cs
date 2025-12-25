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
    private readonly ApplicationDbContext _context;

    public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
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
                UserId = user.Id,
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
}