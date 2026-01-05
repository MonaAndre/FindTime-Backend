using FindTime.Common;
using FindTime.Data;
using FindTime.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Extensions;

public static class ServiceExtensions
{
    public static async Task<(bool isValid, ServiceResponse<T>? ErrorResponse, ApplicationUser? User)>
        ValidateUserAsync<T>(
            this UserManager<ApplicationUser> userManager, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return (false, ServiceResponse<T>.NotFoundResponse("User not found"), null);
        }

        return (true, null, user);
    }

    public static async Task<(bool IsValid, ServiceResponse<T>? ErrorResponse)> ValidateUserIsGroupAdminAsync<T>(
        this ApplicationDbContext context, int groupId, string userId)
    {
        var group = await context.Groups.FindAsync(groupId);
        if (group == null || group.IsDeleted)
        {
            return (false, ServiceResponse<T>.NotFoundResponse("Group is not found"));
        }

        if (group.AdminId != userId)
        {
            return (false, ServiceResponse<T>.ForbiddenResponse("User is not admin"));
        }

        return (true, null);
    }

    public static async Task<(bool IsValid, ServiceResponse<T>? ErrorResponse, GroupUser? GroupUser)>
        ValidateGroupMemberAsync<T>(
            this ApplicationDbContext context,
            int groupId,
            string groupUserId)
    {
        var groupUser = await context.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == groupUserId
                                                                           && gu.GroupId == groupId);


        if (groupUser == null || !groupUser.IsActive)
        {
            return (false, ServiceResponse<T>.ForbiddenResponse("The user is not a member"), null);
        }


        return (true, null, groupUser);
    }

    public static async Task<(bool IsValid, ServiceResponse<T>? ErrorResponse, Category? Category)>
        ValidateCategoryAsync<T>(
            this ApplicationDbContext context,
            int? categoryId,
            int groupId)
    {
        var groupCategory = await context.Categories.FirstOrDefaultAsync(gc => gc.CategoryId == categoryId
                                                                               && gc.GroupId == groupId);
        if (groupCategory == null)
        {
            return (false, ServiceResponse<T>.ForbiddenResponse("The category doesn't exist"), null);
        }

        return (true, null, groupCategory);
    }
}