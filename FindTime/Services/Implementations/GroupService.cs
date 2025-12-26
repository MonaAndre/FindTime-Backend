using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.GroupDTOs;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

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
            );        }
        catch (Exception e)
        {
            return ServiceResponse<CreateGroupDtoResponse>.ErrorResponse($"Error creating group {e.Message}", 500);
        }
    }
}