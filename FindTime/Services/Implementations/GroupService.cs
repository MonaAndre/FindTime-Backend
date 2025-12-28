using System.Dynamic;
using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.GroupDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Services.Implementations;

public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ServiceResponse<CreateGroupDtoResponse>> CreateGroupAsync(CreateGroupDtoRequest dto,
        string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse("Group name is required.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<CreateGroupDtoResponse>.NotFoundResponse("User not found");
            }

            var newGroup = new Group
            {
                GroupName = dto.GroupName,
                AdminId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Groups.AddAsync(newGroup);
            await _context.SaveChangesAsync();

            var adminGroupUser = new GroupUser
            {
                GroupId = newGroup.GroupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _context.GroupUsers.AddAsync(adminGroupUser);

            var addedMembers = new List<GroupMemberDto>
            {
                new GroupMemberDto
                {
                    Email = user.Email!,
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

                    var newMember = await _userManager.FindByEmailAsync(email);
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
                    await _context.GroupUsers.AddAsync(foundUser);

                    addedMembers.Add(new GroupMemberDto
                    {
                        Email = newMember.Email!,
                        FirstName = newMember.FirstName,
                        LastName = newMember.LastName,
                        IsAdmin = false
                    });
                }
            }

            await _context.SaveChangesAsync();
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

            var group = await _context.Groups.FindAsync(dto.GroupId);
            var user = await _userManager.FindByIdAsync(userId);
            if (group == null || user == null)
            {
                return user == null
                    ? ServiceResponse<bool>.NotFoundResponse("User not found")
                    : ServiceResponse<bool>.NotFoundResponse("Group not found");
            }

            var isMember = await _context.GroupUsers
                .AnyAsync(member => member.UserId == userId && member.GroupId == group.GroupId);
            if (!isMember)
            {
                return ServiceResponse<bool>.NotFoundResponse(
                    "User does not have access to change this group information");
            }

            group.GroupName = dto.GroupName;
            group.Description = dto.Description;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse("Error updating group info", 500);
        }
    }

    public async Task<ServiceResponse<List<GetAllGroupsResponse>>> GetAllGroupsAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<List<GetAllGroupsResponse>>.NotFoundResponse("User not found");
            }

            var userGroups = await _context.GroupUsers
                .Include(member => member.Group)
                .ThenInclude(g => g.Admin)
                .Include(groupUsers => groupUsers.Group.GroupUsers)
                .ThenInclude(groupUsers => groupUsers.User)
                .Where(groupUser => groupUser.UserId == user.Id && groupUser.IsActive && !groupUser.Group.IsDeleted)
                .Select(groupUser => new GetAllGroupsResponse
                {
                    GroupId = groupUser.Group.GroupId,
                    GroupName = groupUser.Group.GroupName,
                    Description = groupUser.Group.Description,
                    AdminEmail = groupUser.Group.AdminId,
                    AdminName = groupUser.Group.Admin.FirstName,
                    IsAdmin = groupUser.Group.AdminId == user.Id,
                    CreatedAt = groupUser.Group.CreatedAt,
                    JoinedAt = groupUser.JoinedAt,
                    MemberCount = groupUser.Group.GroupUsers.Count(m => m.IsActive)
                })
                .ToListAsync();

            return ServiceResponse<List<GetAllGroupsResponse>>.SuccessResponse(userGroups,
                $"Found {userGroups.Count} groups");
        }
        catch (Exception e)
        {
            return ServiceResponse<List<GetAllGroupsResponse>>.ErrorResponse("Error getting groups", 500);
        }
    }

    public async Task<ServiceResponse<GroupInfoDtoResponse>> GetGroupInfoAsync(string userId, int groupId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<GroupInfoDtoResponse>.NotFoundResponse("User not found");
            }

            var member = await _context.GroupUsers
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

            var members = await _context.GroupUsers
                .Include(member => member.User)
                .Where(gu => gu.GroupId == groupId && gu.IsActive)
                .Select(gu => new GroupMemberGroupDto
                {
                    UserId = gu.UserId,
                    Email = gu.User.Email!,
                    FirstName = gu.User.FirstName,
                    LastName = gu.User.LastName,
                    JoinedAt = gu.JoinedAt,
                    ProfilePictureLink = gu.User.ProfilePicLink,
                    IsAdmin = gu.UserId == user.Id
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
                IsAdmin = group.AdminId == user.Id,
                CreatedAt = group.CreatedAt,
                JoinedAt = member.JoinedAt,
                AdminName = group.Admin.FirstName,
                MemberCount = members.Count
            };
            return ServiceResponse<GroupInfoDtoResponse>.SuccessResponse(groupInfo);
        }
        catch (Exception e)
        {
            return ServiceResponse<GroupInfoDtoResponse>.ErrorResponse("Failed to get group info", 500);
        }
    }

    public async Task<ServiceResponse<bool>> AddMemberToGroupAsync(AddMemberToGroupDtoRequest dto, string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("User not found");
            }

            var isAdmin = await _context.Groups
                .AnyAsync(adm => adm.AdminId == userId);

            if (!isAdmin)
            {
                return ServiceResponse<bool>.ForbiddenResponse("The user is not an admin");
            }

            if (string.IsNullOrWhiteSpace(dto.UserEmail))
            {
                return ServiceResponse<bool>.ErrorResponse("Email can not be empty");
            }

            var newMember = await _userManager.FindByEmailAsync(dto.UserEmail);
            if (newMember == null || newMember.IsDeleted)
            {
                return ServiceResponse<bool>.NotFoundResponse("User does not exist or was deleted");
            }

            var isMember = await _context.GroupUsers
                .AnyAsync(member => member.UserId == newMember.Id && member.GroupId == dto.GroupId);
            if (isMember)
            {
                return ServiceResponse<bool>.ErrorResponse(
                    "User is already a member in this group");
            }

            var newUserGroup = new GroupUser
            {
                UserId = newMember.Id,
                GroupId = dto.GroupId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _context.GroupUsers.AddAsync(newUserGroup);
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch (Exception e)
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to add new member", 500);
        }
    }

    public async Task<ServiceResponse<bool>> DeleteMember(DeleteMemberDtoRequest dto, string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse<bool>.NotFoundResponse("User not found");
            }

            var isAdmin = await _context.Groups
                .AnyAsync(adm => adm.AdminId == userId);
            if (!isAdmin)
            {
                return ServiceResponse<bool>.ForbiddenResponse("The user is not an admin");
            }

            var userToDelete = await _context.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == dto.UserId
                && gu.GroupId == dto.GroupId);


            if (userToDelete == null || !userToDelete.IsActive)
            {
                return ServiceResponse<bool>.ForbiddenResponse("The user is not a member");
            }

            userToDelete.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResponse(true);
        }
        catch
        {
            return ServiceResponse<bool>.ErrorResponse("Failed to delete member", 500);
        }
    }
}

// leave group
// change admin
