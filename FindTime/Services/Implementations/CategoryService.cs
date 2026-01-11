using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.CategoryDto;
using FindTime.DTOs.CategoryDTOs;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
                    cat.Name.Trim().ToLower() == dto.CategoryName!.Trim().ToLower() && cat.GroupId == dto.GroupId);
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
    public async Task<ServiceResponse<bool>> AddOrUpdateCategoryToEventAsync(AddCategoryToEventDtoRequest dto, string userId)
    {
        try
        {

            var (isValidUser, errorResponsesUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponsesUser!;
            }
            var (isValidMember, errorResponseMember, member) = await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isValidMember || member == null)
            {
                return errorResponseMember!;
            }

            var validCategoryId = await context.Categories.FirstOrDefaultAsync(vc => vc.CategoryId == dto.CategoryId && vc.GroupId == dto.GroupId);
            if (validCategoryId == null)
            {
                return ServiceResponse<bool>.ErrorResponse("Category not found");
            }
            var foundEvent = await context.Events.FirstOrDefaultAsync(e => e.EventId == dto.EventId && e.GroupId == dto.GroupId);
            if (foundEvent == null)
            {
                return ServiceResponse<bool>.ErrorResponse("Event not found");
            }

            if (foundEvent.IsRecurring == true)
            {
                int updatedCount = 0;
                int masterEventId = foundEvent.RecurringGroupId ?? foundEvent.EventId;

                var masterEvent = await context.Events
                    .FirstOrDefaultAsync(e => e.EventId == masterEventId && e.GroupId == dto.GroupId);
                if (masterEvent != null)
                {
                    masterEvent.CategoryId = dto.CategoryId;
                    updatedCount++;
                }

                var recurringEvents = await context.Events
                .Where(re => re.GroupId == dto.GroupId && re.RecurringGroupId == masterEventId)
                .ToListAsync();


                foreach (var recurringEvent in recurringEvents)
                {
                    recurringEvent.CategoryId = dto.CategoryId;
                }
                updatedCount += recurringEvents.Count;

                await context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResponse(true, $"Categories updated on {updatedCount} events");
            }

            foundEvent!.CategoryId = dto.CategoryId;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true, $"Category updated for event {foundEvent?.Category?.Name}");
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to add or update category{e.Message}");
        }
    }

    public async Task<ServiceResponse<List<CategoryListDtoResponse>>> GetAllCategoriesAsync(int groupId, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<List<CategoryListDtoResponse>>(userId);
            if (!isValidUser && user == null)
            {
                return errorResponseUser!;
            }
            var (isMember, errorMember, member) = await context.ValidateGroupMemberAsync<List<CategoryListDtoResponse>>(groupId, userId);
            if (!isMember && member == null)
            {
                return errorMember!;
            }

            var categoryList = await context.Categories.Where(cl => cl.GroupId == groupId && cl.IsDeleted == false).Select(
                c => new CategoryListDtoResponse
                {
                    CategoryName = c.Name,
                    CategoryColor = c.Color,
                    CategoryId = c.CategoryId
                }).ToListAsync();
            return ServiceResponse<List<CategoryListDtoResponse>>.SuccessResponse(categoryList, $"Category list fetched with {categoryList.Count} categorie(s)");

        }
        catch (Exception e)
        {

            return ServiceResponse<List<CategoryListDtoResponse>>.ErrorResponse(e.Message, 500);
        }
    }
    public async Task<ServiceResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequestDto dto, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName) || string.IsNullOrWhiteSpace(dto.CategoryColor))
            {
                return ServiceResponse<bool>.ErrorResponse("Category name or color is empty");
            }
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser && user == null)
            {
                return errorResponseUser!;
            }
            var (isMember, errorMember, member) = await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isMember && member == null)
            {
                return errorMember!;
            }
            var (isCategory, categoryError, category) = await context.ValidateCategoryAsync<bool>(dto.CategoryId, dto.GroupId);
            if (!isCategory || category == null) return categoryError!;

            var categoryToUpdate = await context.Categories.FirstOrDefaultAsync(cu => cu.CategoryId == dto.CategoryId && cu.GroupId == dto.GroupId && cu.IsDeleted == false);
            categoryToUpdate!.Name = dto.CategoryName!;
            categoryToUpdate!.Color = dto.CategoryColor!;

            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true, "Category updated successfully");
        }
        catch (Exception e)
        {

            return ServiceResponse<bool>.ErrorResponse(e.Message, 500);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteCategoryAsync(DeleteCategoryDtoRequest dto, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser && user == null)
            {
                return errorResponseUser!;
            }
            var (isMember, errorMember, member) = await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isMember && member == null)
            {
                return errorMember!;
            }
            var (isCategory, categoryError, category) = await context.ValidateCategoryAsync<bool>(dto.CategoryId, dto.GroupId);
            if (!isCategory || category == null) return categoryError!;

            var categoryToDelete = await context.Categories.FirstOrDefaultAsync(cd => cd.CategoryId == dto.CategoryId && cd.GroupId == dto.GroupId);
            if (categoryToDelete?.IsDeleted == true) return ServiceResponse<bool>.ErrorResponse("The category is already deleted");

            categoryToDelete!.IsDeleted = true;
            categoryToDelete.DeletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResponse(true, $"Category {categoryToDelete?.Name} was deleted");

        }
        catch (Exception e)
        {

            return ServiceResponse<bool>.ErrorResponse(e.Message, 500);
        }
    }


}