using System.Text.RegularExpressions;
using FindTime.Common;
using FindTime.DTOs.AuthDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FindTime.Services.Implementations;

public class AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    : IAuthService
{
    public async Task<ServiceResponse<AuthResponseDto>> RegisterAsync(RegisterDtoRequest dto)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResponse<AuthResponseDto>.ErrorResponse("A user with this email already exist");
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
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResponse<AuthResponseDto>.ErrorResponse("Failed to create user", 400, errors);
            }

            var response = new AuthResponseDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                ProfilePictureLink = null
            };

            return ServiceResponse<AuthResponseDto>.SuccessResponse(response, "User created");
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
            var user = await userManager.FindByEmailAsync(dto.Email);
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

            var result = await signInManager.PasswordSignInAsync(user, dto.Password, true, false);
            if (!result.Succeeded)
                return ServiceResponse<AuthResponseDto>.ErrorResponse("Invalid email or password ", 401);

            var response = new AuthResponseDto
            {
                Id= user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureLink = user.ProfilePicLink
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
            await signInManager.SignOutAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse(e.Message, statusCode: 500);
        }
    }

    public async Task<ServiceResponse<bool>> ChangePasswordAsync(ChangePasswordRequestDto dto, string userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("User not found");
            }

            var isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return ServiceResponse<bool>.ErrorResponse("Current password is incorrect");
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return ServiceResponse<bool>.ErrorResponse("New password does not match with confirm new password",
                    statusCode: 400);
            }

            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$";
            if (!Regex.IsMatch(dto.NewPassword, pattern))
            {
                return ServiceResponse<bool>.ErrorResponse(
                    "Password need to include minimum 8 characters, 1 big letter, 1 small letter and 1 special character");
            }

            var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                return ServiceResponse<bool>.ErrorResponse("Failed to change password, try again later", 500);
            }

            await userManager.UpdateSecurityStampAsync(user);
            await signInManager.RefreshSignInAsync(user);
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception)
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to change password", 500);
        }
    }

    // todo för senare när kopplar mejl:
    // reset password
    // 2fa
    // confirm user
}