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
                CreatedAt = DateTime.UtcNow,
                Description = dto.Description
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
            var isCreatedUserSet = await CreateDefUserGroupSet(userId, newGroup.GroupId);
            if (!isCreatedUserSet)
            {
                return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse("Failed to add user group settings");
            }


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
                    var isCreated = await CreateDefUserGroupSet(newMember.Id, newGroup.GroupId);
                    if (!isCreated)
                    {
                        return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse("Group creation failed.");
                    }
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


            var userGroupSettings =
                await context.UserGroupSettings.FirstOrDefaultAsync(us => us.UserId == userId && us.GroupId == groupId);

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
                    IsAdmin = gu.UserId == user!.Id,
                    Nickname = context.UserMemberSettings
                        .Where(ums => ums.UserId == userId
                                      && ums.TargetUserId == gu.UserId
                                      && ums.GroupId == groupId)
                        .Select(ums => ums.Nickname)
                        .FirstOrDefault()
                })
                .OrderByDescending(m => m.IsAdmin)
                .ThenBy(m => m.FirstName)
                .ToListAsync();
            var groupInfo = new GroupInfoDtoResponse
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                Description = group.Description,
                UserGroupColor = userGroupSettings!.GroupColor,
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
            var isCreated = await CreateDefUserGroupSet(newMember.Id, dto.GroupId);
            return !isCreated
                ? ServiceResponse<bool>.ErrorResponse("Failed to add user settings")
                : ServiceResponse<bool>.SuccessResponse(true, "Member added to the group");
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

            if (dto.UserId == userId)
            {
                return ServiceResponse<bool>.ErrorResponse("You cannot remove yourself");
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

            var remainingActiveMembers =
                await context.GroupUsers.CountAsync(gu => gu.GroupId == groupId && gu.IsActive && gu.UserId != userId);

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(groupId, userId);
            if (isValidAdmin && remainingActiveMembers > 0)
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


            if (remainingActiveMembers == 0)
            {
                var group = await context.Groups.FindAsync(groupId);
                if (group != null)
                {
                    group.IsDeleted = true;
                    group.DeletedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResponse(true,
                    "You were the last member. The group would be deleted automatically");
            }

            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to leave group:{e}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteGroupAsync(int groupId, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponseUser!;
            }

            var currentGroup = await context.Groups.FindAsync(groupId);
            if (currentGroup == null || currentGroup.IsDeleted)
            {
                return ServiceResponse<bool>.NotFoundResponse("Group not found");
            }

            var (isValidGroupMember, errorResponseGroupMember, member) =
                await context.ValidateGroupMemberAsync<bool>(groupId, userId);
            if (!isValidGroupMember || member == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("Group not found");
            }

            var (isValidAdmin, errorResponseAdmin) =
                await context.ValidateUserIsGroupAdminAsync<bool>(groupId, userId);
            if (!isValidAdmin)
            {
                return errorResponseAdmin!;
            }

            currentGroup.IsDeleted = true;
            await context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to delete group:{e}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> AddNicknameToGroupMemberAsync(AddNicknameDtoRequest dto, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nickname))
            {
                return ServiceResponse<bool>.NotFoundResponse("Nickname is required");
            }

            var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
            if (!isValidUser || user == null)
            {
                return errorResponseUser!;
            }

            var (isValidTargetUser, errorResponseTargetUser, targetUser) =
                await userManager.ValidateUserAsync<bool>(dto.TargetUserId);

            if (!isValidTargetUser || targetUser == null)
            {
                return errorResponseTargetUser!;
            }

            var (isValidGroupMember, errorResponseGroupMember, member) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
            if (!isValidGroupMember || member == null)
            {
                return errorResponseGroupMember!;
            }

            var (isValidTargetMember, errorResponseGroupTargetM, targetMember) =
                await context.ValidateGroupMemberAsync<bool>(dto.GroupId, dto.TargetUserId);
            if (!isValidTargetMember || targetMember == null)
            {
                return errorResponseGroupTargetM!;
            }


            var existingUserMemberSetting =
                await context.UserMemberSettings.FirstOrDefaultAsync(ex =>
                    ex.UserId == userId && ex.TargetUserId == dto.TargetUserId && ex.GroupId == dto.GroupId);
            if (existingUserMemberSetting != null)
            {
                existingUserMemberSetting.Nickname = dto.Nickname;
                existingUserMemberSetting.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResponse(true, $"Nickname {dto.Nickname} updated");
            }

            if (existingUserMemberSetting == null)
            {
                var newUserMemberSetting = new UserMemberSettings
                {
                    UserId = userId,
                    TargetUserId = targetUser.Id,
                    GroupId = dto.GroupId,
                    Nickname = dto.Nickname,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await context.UserMemberSettings.AddAsync(newUserMemberSetting);
                await context.SaveChangesAsync();
            }

            return ServiceResponse<bool>.SuccessResponse(true,
                $"New nickname:{dto.Nickname} added to group:{dto.GroupId}");
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse($"Failed to add nickname:{e}", 500);
        }
    }

    public async Task<ServiceResponse<bool>> UpdateUserGroupSettings(UpdateUserGroupSettingsDtoRequest dto,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.GroupColor) ||
            !System.Text.RegularExpressions.Regex.IsMatch(dto.GroupColor, "^#([A-Fa-f0-9]{6})$"))
        {
            return ServiceResponse<bool>.ErrorResponse("Invalid color format. Use hex format like #ffffff");
        }

        var (isValidUser, errorResponseUser, user) = await userManager.ValidateUserAsync<bool>(userId);
        if (!isValidUser || user == null)
        {
            return errorResponseUser!;
        }

        var (isValidGroupMember, errorResponseGroupMember, member) =
            await context.ValidateGroupMemberAsync<bool>(dto.GroupId, userId);
        if (!isValidGroupMember || member == null)
        {
            return errorResponseGroupMember!;
        }

        var updatedUserGroupSettings =
            await context.UserGroupSettings.FirstOrDefaultAsync(g => g.UserId == userId && g.GroupId == dto.GroupId);
        if (updatedUserGroupSettings == null)
        {
            return ServiceResponse<bool>.NotFoundResponse("User group settings are not found");
        }

        updatedUserGroupSettings.GroupColor = dto.GroupColor;
        updatedUserGroupSettings.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return ServiceResponse<bool>.SuccessResponse(true,
            $"The user group settings updated for the color {updatedUserGroupSettings.GroupColor}");
    }


    private async Task<bool> CreateDefUserGroupSet(string userId, int groupId)
    {
        try
        {
            var defUserSettings = new UserGroupSettings
            {
                UserId = userId,
                GroupId = groupId,
                GroupColor = "#ffffff",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.AddAsync(defUserSettings);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}