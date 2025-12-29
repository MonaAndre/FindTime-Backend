using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.GroupDTOs;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Services.Implementations;

public class GroupService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    : IGroupService
{
    public async Task<ServiceResponse<CreateGroupDtoResponse>> CreateGroupAsync(CreateGroupDtoRequest dto,
        string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse("Group name is required.");
            }

            var (isValidUser, errorUser, user) = await userManager.ValidateUserAsync<CreateGroupDtoResponse>(userId);
            if (!isValidUser) return errorUser!;

            var newGroup = new Group
            {
                GroupName = dto.GroupName,
                AdminId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await context.Groups.AddAsync(newGroup);
            await context.SaveChangesAsync();

            var adminGroupUser = new GroupUser
            {
                GroupId = newGroup.GroupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.GroupUsers.AddAsync(adminGroupUser);

            var addedMembers = new List<GroupMemberDto>
            {
                new GroupMemberDto
                {
                    Email = user!.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsAdmin = true
                }
            };
            var failedMembers = new List<string>();

            if (dto.MembersEmails.Count > 0)
            {
                foreach (var email in dto.MembersEmails)
                {
                    if (email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var newMember = await userManager.FindByEmailAsync(email);
                    if (newMember == null || newMember.IsDeleted)
                    {
                        failedMembers.Add(email);
                        continue;
                    }


                    var foundUser = new GroupUser
                    {
                        GroupId = newGroup.GroupId,
                        UserId = newMember.Id,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await context.GroupUsers.AddAsync(foundUser);

                    addedMembers.Add(new GroupMemberDto
                    {
                        Email = newMember.Email!,
                        FirstName = newMember.FirstName,
                        LastName = newMember.LastName,
                        IsAdmin = false
                    });
                }
            }

            await context.SaveChangesAsync();
            var response = new CreateGroupDtoResponse
            {
                GroupId = newGroup.GroupId,
                GroupName = newGroup.GroupName,
                AdminEmail = user.Email!,
                Members = addedMembers,
                FailedEmails = failedMembers
            };
            return ServiceResponse<CreateGroupDtoResponse>.SuccessResponse(
                response,
                failedMembers.Count > 0
                    ? $"Group created successfully. {failedMembers.Count} email(s) could not be added."
                    : "Group created successfully"
            );
        }
        catch (Exception e)
        {
            return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse($"Error creating group {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> UpdateGroupInfoAsync(UpdateGroupInfoDtoRequest dto, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                return ServiceResponse<bool>.ErrorResponse("Group name can not be empty");
            }

            var group = await context.Groups.FindAsync(dto.GroupId);
            var user = await userManager.FindByIdAsync(userId);
            if (group == null || user == null)
            {
                return user == null
                    ? ServiceResponse<bool>.NotFoundResponse("User not found")
                    : ServiceResponse<bool>.NotFoundResponse("Group not found");
            }

            var (isValidMember, errorResponseMember, member) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isValidMember || member == null) return errorResponseMember!;

            group.GroupName = dto.GroupName;
            group.Description = dto.Description;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception)
        {
            return ServiceResponse<bool>.ErrorResponse("Error updating group info", 500);
        }
    }

    public async Task<ServiceResponse<List<GetAllGroupsResponse>>> GetAllGroupsAsync(string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<List<GetAllGroupsResponse>>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var userGroups = await context.GroupUsers
                .Include(member => member.Group)
                .ThenInclude(g => g.Admin)
                .Include(groupUsers => groupUsers.Group.GroupUsers)
                .ThenInclude(groupUsers => groupUsers.User)
                .Where(groupUser => groupUser.UserId == user!.Id && groupUser.IsActive && !groupUser.Group.IsDeleted)
                .Select(groupUser => new GetAllGroupsResponse
                {
                    GroupId = groupUser.Group.GroupId,
                    GroupName = groupUser.Group.GroupName,
                    Description = groupUser.Group.Description,
                    AdminEmail = groupUser.Group.AdminId,
                    AdminName = groupUser.Group.Admin.FirstName,
                    IsAdmin = groupUser.Group.AdminId == user!.Id,
                    CreatedAt = groupUser.Group.CreatedAt,
                    JoinedAt = groupUser.JoinedAt,
                    MemberCount = groupUser.Group.GroupUsers.Count(m => m.IsActive)
                })
                .ToListAsync();

            return ServiceResponse<List<GetAllGroupsResponse>>.SuccessResponse(userGroups,
                $"Found {userGroups.Count} groups");
        }
        catch (Exception)
        {
            return ServiceResponse<List<GetAllGroupsResponse>>.ErrorResponse("Error getting groups", 500);
        }
    }

    public async Task<ServiceResponse<GroupInfoDtoResponse>> GetGroupInfoAsync(string userId, int groupId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<GroupInfoDtoResponse>(userId);
            if (!isValidUser) return errorResponseUser!;

            var member = await context.GroupUsers
                .Include(member => member.Group)
                .ThenInclude(g => g.Admin)
                .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId && gu.IsActive);

            if (member == null)
            {
                return ServiceResponse<GroupInfoDtoResponse>.ForbiddenResponse("Not a member of this group");
            }

            var group = member.Group;
            if (group.IsDeleted)
            {
                return ServiceResponse<GroupInfoDtoResponse>.NotFoundResponse("Group is deleted");
            }

            var members = await context.GroupUsers
                .Include(m => m.User)
                .Where(gu => gu.GroupId == groupId && gu.IsActive)
                .Select(gu => new GroupMemberGroupDto
                {
                    UserId = gu.UserId,
                    Email = gu.User.Email!,
                    FirstName = gu.User.FirstName,
                    LastName = gu.User.LastName,
                    JoinedAt = gu.JoinedAt,
                    ProfilePictureLink = gu.User.ProfilePicLink,
                    IsAdmin = gu.UserId == user!.Id
                })
                .OrderByDescending(m => m.IsAdmin)
                .ThenBy(m => m.FirstName)
                .ToListAsync();
            var groupInfo = new GroupInfoDtoResponse
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                Description = group.Description,
                AdminEmail = group.Admin.Email!,
                Members = members,
                IsAdmin = group.AdminId == user!.Id,
                CreatedAt = group.CreatedAt,
                JoinedAt = member.JoinedAt,
                AdminName = group.Admin.FirstName,
                MemberCount = members.Count
            };
            return ServiceResponse<GroupInfoDtoResponse>.SuccessResponse(groupInfo);
        }
        catch (Exception)
        {
            return ServiceResponse<GroupInfoDtoResponse>.ErrorResponse("Failed to get group info", 500);
        }
    }

    public async Task<ServiceResponse<bool>> AddMemberToGroupAsync(AddMemberToGroupDtoRequest dto, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.UserEmail))
            {
                return ServiceResponse<bool>.ErrorResponse("Email cannot be empty");
            }

            var group = await context.Groups.FindAsync(dto.GroupId);
            if (group == null || group.IsDeleted)
            {
                return ServiceResponse<bool>.ErrorResponse("Group not found");
            }

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(dto.GroupId, userId);
            if (!isValidAdmin)
            {
                return errorResponseAdmin!;
            }

            var newMember = await userManager.FindByEmailAsync(dto.UserEmail);
            if (newMember == null || newMember.IsDeleted)
            {
                return ServiceResponse<bool>.NotFoundResponse("User does not exist or was deleted");
            }

            var exMember = await context.GroupUsers
                .FirstOrDefaultAsync(gu => gu.GroupId == dto.GroupId && gu.UserId == newMember.Id);
            if (exMember != null)
            {
                if (exMember.IsActive)
                {
                    return ServiceResponse<bool>.ErrorResponse("Member already exists in this group");
                }

                exMember.IsActive = true;
                exMember.JoinedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResponse(true, "Member re-added to the group");
            }

            var newUserGroup = new GroupUser
            {
                UserId = newMember.Id,
                GroupId = dto.GroupId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await context.GroupUsers.AddAsync(newUserGroup);
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true, "Member added to the group");
        }
        catch (Exception)
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to add new member", 500);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteMemberAsync(DeleteMemberDtoRequest dto, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponseUser!;
            }

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(dto.GroupId, userId);
            if (!isValidAdmin)
            {
                return errorResponseAdmin!;
            }

            var (isValidGroupMember, errorResponseGroup, userToDelete) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, dto.UserId);
            if (!isValidGroupMember)
            {
                return errorResponseGroup!;
            }

            userToDelete!.IsActive = false;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to delete member", 500);
        }
    }

    public async Task<ServiceResponse<bool>> ChangeAdminAsync(ChangeAdminDtoRequest dto, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponseUser!;
            }

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(dto.GroupId, userId);
            if (!isValidAdmin)
            {
                return errorResponseAdmin!;
            }

            var (isValidMember, errorResponseMember, member) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, dto.NewAdminUserId);
            if (!isValidMember || member == null)
            {
                return errorResponseMember!;
            }

            var currentGroup = await context.Groups.FindAsync(dto.GroupId);
            if (currentGroup == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("Group not found");
            }

            currentGroup.AdminId = dto.NewAdminUserId;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true, "Admin changed");
        }
        catch (Exception)
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to change admin", 500);
        }
    }

    public async Task<ServiceResponse<bool>> LeaveGroupAsync(int groupId, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponseUser!;
            }

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(groupId, userId);
            if (isValidAdmin)
            {
                return ServiceResponse<bool>.ErrorResponse(
                    "You can not leave the group because you are admin of this group, please change admin first or delete this group");
            }

            var (isValidGroupMember, errorResponseGroupMember, memberWhoLeave) =
                await context.ValidateGroupMemberAsync<bool>(groupId, userId);
            if (!isValidGroupMember)
            {
                return errorResponseGroupMember!;
            }

            memberWhoLeave!.IsActive = false;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to leave group:{e}", 500);
        }
    }
    
    
}
// för admin delete group hela group och eventer tas bort members ser inte längre den group som active