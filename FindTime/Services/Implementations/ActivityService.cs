using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.ActivityDTOs;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Services.Implementations;

public class ActivityService(ApplicationDbContext context) : IActivityService
{
    public async Task LogActivityAsync(
        ActivityType type,
        string actorUserId,
        int groupId,
        string groupName,
        int? eventId,
        string? eventName)
    {
        try
        {
            var activity = new Activity
            {
                Type = type,
                ActorUserId = actorUserId,
                GroupId = groupId,
                GroupName = groupName,
                EventId = eventId,
                EventName = eventName,
                CreatedAt = DateTime.UtcNow
            };

            context.Activities.Add(activity);
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to log activity: {e.Message}");
        }
    }

    public async Task<ServiceResponse<List<ActivityDtoResponse>>> GetGroupActivityAsync(
        int groupId,
        string userId,
        int page,
        int pageSize)
    {
        try
        {
            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<List<ActivityDtoResponse>>(groupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var activities = await context.Activities
                .Where(a => a.GroupId == groupId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityDtoResponse
                {
                    ActivityId = a.ActivityId,
                    Type = a.Type,
                    ActorUserId = a.ActorUserId,
                    ActorFirstName = a.ActorUser.FirstName,
                    ActorLastName = a.ActorUser.LastName,
                    GroupId = a.GroupId,
                    GroupName = a.GroupName,
                    EventId = a.EventId,
                    EventName = a.EventName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return ServiceResponse<List<ActivityDtoResponse>>.SuccessResponse(activities);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<ActivityDtoResponse>>.ErrorResponse(
                $"Failed to get group activity: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<List<ActivityDtoResponse>>> GetUserActivityAsync(
        string userId,
        int page,
        int pageSize)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var activities = await context.Activities
                .Where(a => a.Group.GroupUsers.Any(gu => gu.UserId == userId && gu.IsActive))
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityDtoResponse
                {
                    ActivityId = a.ActivityId,
                    Type = a.Type,
                    ActorUserId = a.ActorUserId,
                    ActorFirstName = a.ActorUser.FirstName,
                    ActorLastName = a.ActorUser.LastName,
                    GroupId = a.GroupId,
                    GroupName = a.GroupName,
                    EventId = a.EventId,
                    EventName = a.EventName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return ServiceResponse<List<ActivityDtoResponse>>.SuccessResponse(activities);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<ActivityDtoResponse>>.ErrorResponse(
                $"Failed to get user activity: {e.Message}", 500);
        }
    }
}
