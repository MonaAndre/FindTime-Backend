using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.CategoryDto;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Services.Implementations;

public class CategoryService(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : ICategoryService
{
    public async Task<ServiceResponse<bool>> CreateCategoryAsync(CreateCategoryDtoRequest dto, string userId)
    {
        try
        {
            var (isValidUser, errorUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorUser!;
            }

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isValidMember || member == null)
                return errorMember!;
            var existingCategoryName =
                await context.Categories.FirstOrDefaultAsync(cat =>
                    cat.Name.Trim().ToLower() == dto.CategoryName!.Trim().ToLower());
            if (existingCategoryName != null)
            {
                return ServiceResponse<bool>.ErrorResponse("The category name already exists.", 409);
            }

            var newCategory = new Category
            {
                Name = dto.CategoryName!.Trim(),
                Color = dto.CategoryColor!.Trim(),
                GroupId = dto.GroupId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
            };
            
            await context.Categories.AddAsync(newCategory);
            await context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResponse(true, $"New category created {newCategory.Name} ");
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse(e.Message, 500);
        }
    }
}