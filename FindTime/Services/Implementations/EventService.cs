using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.EventDTOs;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FindTime.Services.Implementations;

public class EventService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    INotificationService notificationService,
    IActivityService activityService) : IEventService
{
    public async Task<ServiceResponse<CreateEventDtoResponse>> CreateEventAsync(CreateEventDtoRequest dto,
        string userId)
    {
        try
        {
            var (isValidMember, errorResponseGroupMem, groupMember) =
                await context.ValidateGroupMemberAsync<CreateEventDtoResponse>(dto.GroupId, userId);
            if (!isValidMember || groupMember == null)
            {
                return errorResponseGroupMem!;
            }

            if (string.IsNullOrWhiteSpace(dto.EventName))
            {
                return ServiceResponse<CreateEventDtoResponse>.ErrorResponse("Event name can not be empty");
            }

            if (dto.EndTime < dto.StartTime)
            {
                return ServiceResponse<CreateEventDtoResponse>.ErrorResponse("Event can't be after start time");
            }

            if (dto.CategoryId != null)
            {
                var (isValidCategory, errorResponseCategory, groupCategory) =
                    await context.ValidateCategoryAsync<CreateEventDtoResponse>(dto.CategoryId, dto.GroupId);
                if (!isValidCategory || groupCategory == null)
                {
                    return errorResponseCategory!;
                }
            }

            if (dto.IsRecurring)
            {
                if (dto.RecurrencePattern == null)
                {
                    return ServiceResponse<CreateEventDtoResponse>.ErrorResponse("Recurrence pattern can not be empty");
                }

                if (!Enum.IsDefined(typeof(RecurrencePattern), dto.RecurrencePattern))
                {
                    return ServiceResponse<CreateEventDtoResponse>.ErrorResponse(
                        "Invalid recurrence pattern. Valid values are: Daily, Weekly, Monthly, Yearly");
                }

                if (dto.RecurrenceEndTime.HasValue)
                {
                    if (dto.RecurrenceEndTime.Value <= dto.StartTime)
                    {
                        return ServiceResponse<CreateEventDtoResponse>.ErrorResponse(
                            "Recurrence end date must be after the start time");
                    }

                    var maxEndDate = dto.StartTime.AddYears(10);
                    if (dto.RecurrenceEndTime.Value > maxEndDate)
                    {
                        return ServiceResponse<CreateEventDtoResponse>.ErrorResponse(
                            "Recurrence end date cannot be more than 10 years in the future");
                    }
                }
            }

            var newEvent = new Event
            {
                EventName = dto.EventName,
                EventDescription = dto.EventDescription,
                GroupId = dto.GroupId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CategoryId = dto.CategoryId,
                Location = dto.Location,
                CreatorUserId = userId,
                IsRecurring = dto.IsRecurring,
                RecurrencePattern = dto.RecurrencePattern,
                RecurrenceEndTime = dto.RecurrenceEndTime,
                RecurringGroupId = null,
                CreatedAt = DateTime.UtcNow,
            };

            await context.Events.AddAsync(newEvent);
            await context.SaveChangesAsync();

            var memberIds = await context.GroupUsers
                .Where(gu => gu.GroupId == dto.GroupId && gu.IsActive)
                .Select(gu => gu.UserId)
                .ToListAsync();

            context.EventParticipants.AddRange(BuildEventParticipants(newEvent.EventId, memberIds, userId));
            await context.SaveChangesAsync();

            int recurringInstancesCreated = 0;
            if (dto.IsRecurring && dto.RecurrencePattern.HasValue)
            {
                recurringInstancesCreated = await CreateRecurringEventsAsync(
                    dto.RecurrencePattern.Value,
                    newEvent,
                    memberIds,
                    userId);

                if (recurringInstancesCreated == 0)
                {
                    Console.WriteLine(
                        $"Warning: Failed to create recurring instances for event {newEvent.EventId}");
                }
            }

            var creatorName = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FirstName)
                .FirstOrDefaultAsync() ?? "Someone";
            var groupName = await context.Groups
                .Where(g => g.GroupId == dto.GroupId)
                .Select(g => g.GroupName)
                .FirstOrDefaultAsync() ?? "your group";

            await notificationService.NotifyManyAsync(
                memberIds.Where(id => id != userId),
                NotificationType.EventCreated,
                $"{creatorName} created \"{newEvent.EventName}\" in {groupName}",
                newEvent.EventId,
                newEvent.GroupId);

            await activityService.LogActivityAsync(
                ActivityType.EventCreated,
                userId,
                newEvent.GroupId,
                groupName,
                newEvent.EventId,
                newEvent.EventName);

            var createdEvent = new CreateEventDtoResponse
            {
                EventId = newEvent.EventId,
                EventName = newEvent.EventName,
                EventDescription = newEvent.EventDescription,
                GroupId = newEvent.GroupId,
                StartTime = newEvent.StartTime,
                EndTime = newEvent.EndTime,
                CategoryId = newEvent.CategoryId,
                Location = newEvent.Location,
                IsRecurring = newEvent.IsRecurring,
                RecurrencePattern = newEvent.RecurrencePattern,
                RecurrenceEndTime = newEvent.RecurrenceEndTime,

                RecurringInstancesCreated = recurringInstancesCreated > 0
                    ? recurringInstancesCreated
                    : null
            };

            return ServiceResponse<CreateEventDtoResponse>.SuccessResponse(
                createdEvent,
                dto.IsRecurring
                    ? $"Event created with {recurringInstancesCreated} recurring instances"
                    : "Event created");
        }
        catch (Exception e)
        {
            return ServiceResponse<CreateEventDtoResponse>.ErrorResponse($"Failed to create event: {e.Message}");
        }
    }

    public async Task<ServiceResponse<UpdateEventDtoResponse>> UpdateEventAsync(
        UpdateEventDtoRequest dto,
        string userId)
    {
        try
        {
            var eventToUpdate = await context.Events
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.EventId == dto.EventId && !e.IsDeleted);

            if (eventToUpdate == null)
            {
                return ServiceResponse<UpdateEventDtoResponse>.NotFoundResponse("Event not found");
            }

            var (isValidMember, errorResponseGroupMem, groupMember) =
                await context.ValidateGroupMemberAsync<UpdateEventDtoResponse>(
                    eventToUpdate.GroupId,
                    userId);
            if (!isValidMember || groupMember == null)
            {
                return errorResponseGroupMem!;
            }

            var isAdmin = eventToUpdate.Group.AdminId == userId;
            var isCreator = eventToUpdate.CreatorUserId == userId;

            if (!isAdmin && !isCreator)
            {
                return ServiceResponse<UpdateEventDtoResponse>.ForbiddenResponse(
                    "Only the event creator or group admin can update this event");
            }

            if (string.IsNullOrWhiteSpace(dto.EventName))
            {
                return ServiceResponse<UpdateEventDtoResponse>.ErrorResponse("Event name cannot be empty");
            }

            if (dto.EndTime <= dto.StartTime)
            {
                return ServiceResponse<UpdateEventDtoResponse>.ErrorResponse(
                    "End time must be after start time");
            }

            if (dto.CategoryId != null)
            {
                var (isValidCategory, errorResponseCategory, groupCategory) =
                    await context.ValidateCategoryAsync<UpdateEventDtoResponse>(
                        dto.CategoryId,
                        eventToUpdate.GroupId);
                if (!isValidCategory || groupCategory == null)
                {
                    return errorResponseCategory!;
                }
            }

            var updatedCount = 0;

            switch (dto.UpdateOption)
            {
                case UpdateRecurringOption.ThisEventOnly:
                    UpdateSingleEvent(eventToUpdate, dto);
                    updatedCount = 1;
                    break;

                case UpdateRecurringOption.ThisAndFutureEvents:
                    var newStartTime = dto.StartTime.TimeOfDay;
                    var newEndTime = dto.EndTime.TimeOfDay;

                    if (eventToUpdate.RecurringGroupId.HasValue)
                    {
                        var futureEvents = await context.Events
                            .Where(e => e.RecurringGroupId == eventToUpdate.RecurringGroupId
                                        && e.StartTime >= eventToUpdate.StartTime
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in futureEvents)
                        {
                            if (evt.EventId == eventToUpdate.EventId)
                            {
                                UpdateSingleEvent(evt, dto);
                            }
                            else
                            {
                                UpdateEventWithTimeOfDay(evt, dto, newStartTime, newEndTime);
                            }
                        }

                        updatedCount = futureEvents.Count;
                    }
                    else
                    {
                        UpdateSingleEvent(eventToUpdate, dto);

                        var futureInstances = await context.Events
                            .Where(e => e.RecurringGroupId == eventToUpdate.EventId
                                        && e.StartTime >= eventToUpdate.StartTime
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in futureInstances)
                        {
                            UpdateEventWithTimeOfDay(evt, dto, newStartTime, newEndTime);
                        }

                        updatedCount = 1 + futureInstances.Count;
                    }

                    break;

                case UpdateRecurringOption.AllEvents:
                    var allStartTime = dto.StartTime.TimeOfDay;
                    var allEndTime = dto.EndTime.TimeOfDay;

                    if (eventToUpdate.RecurringGroupId.HasValue)
                    {
                        var masterEventId = eventToUpdate.RecurringGroupId.Value;

                        var masterEvent = await context.Events
                            .FirstOrDefaultAsync(e => e.EventId == masterEventId && !e.IsDeleted);

                        if (masterEvent != null)
                        {
                            UpdateEventWithTimeOfDay(masterEvent, dto, allStartTime, allEndTime);
                            updatedCount++;
                        }

                        var allInstances = await context.Events
                            .Where(e => e.RecurringGroupId == masterEventId && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in allInstances)
                        {
                            UpdateEventWithTimeOfDay(evt, dto, allStartTime, allEndTime);
                        }

                        updatedCount += allInstances.Count;
                    }
                    else
                    {
                        UpdateEventWithTimeOfDay(eventToUpdate, dto, allStartTime, allEndTime);
                        updatedCount++;

                        var allInstances = await context.Events
                            .Where(e => e.RecurringGroupId == eventToUpdate.EventId && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in allInstances)
                        {
                            UpdateEventWithTimeOfDay(evt, dto, allStartTime, allEndTime);
                        }

                        updatedCount += allInstances.Count;
                    }

                    break;
            }


            await context.SaveChangesAsync();

            var participantIds = await context.EventParticipants
                .Where(ep => ep.EventId == eventToUpdate.EventId)
                .Select(ep => ep.UserId)
                .ToListAsync();
            var updaterName = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FirstName)
                .FirstOrDefaultAsync() ?? "Someone";
            var updateGroupName = await context.Groups
                .Where(g => g.GroupId == eventToUpdate.GroupId)
                .Select(g => g.GroupName)
                .FirstOrDefaultAsync() ?? "your group";

            await notificationService.NotifyManyAsync(
                participantIds.Where(id => id != userId),
                NotificationType.EventUpdated,
                $"{updaterName} updated \"{eventToUpdate.EventName}\"",
                eventToUpdate.EventId,
                eventToUpdate.GroupId);

            await activityService.LogActivityAsync(
                ActivityType.EventUpdated,
                userId,
                eventToUpdate.GroupId,
                updateGroupName,
                eventToUpdate.EventId,
                eventToUpdate.EventName);

            var response = new UpdateEventDtoResponse
            {
                UpdatedCount = updatedCount,
                Message = dto.UpdateOption switch
                {
                    UpdateRecurringOption.ThisEventOnly => "Event updated successfully",
                    UpdateRecurringOption.ThisAndFutureEvents =>
                        $"{updatedCount} event(s) updated (this and future events)",
                    UpdateRecurringOption.AllEvents =>
                        $"{updatedCount} event(s) updated (all recurring instances)",
                    _ => "Event updated successfully"
                }
            };

            return ServiceResponse<UpdateEventDtoResponse>.SuccessResponse(
                response,
                response.Message);
        }
        catch (Exception e)
        {
            return ServiceResponse<UpdateEventDtoResponse>.ErrorResponse(
                $"Failed to update event: {e.Message}",
                500);
        }
    }

    public async Task<ServiceResponse<DeleteEventDtoResponse>> DeleteEventAsync(
        DeleteEventDtoRequest dto,
        string userId)
    {
        try
        {
            var eventToDelete = await context.Events
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.EventId == dto.EventId && !e.IsDeleted);

            if (eventToDelete == null)
            {
                return ServiceResponse<DeleteEventDtoResponse>.NotFoundResponse("Event not found");
            }

            var (isValidMember, errorResponseGroupMem, groupMember) =
                await context.ValidateGroupMemberAsync<DeleteEventDtoResponse>(
                    eventToDelete.GroupId,
                    userId);
            if (!isValidMember || groupMember == null)
            {
                return errorResponseGroupMem!;
            }

            var isAdmin = eventToDelete.Group.AdminId == userId;
            var isCreator = eventToDelete.CreatorUserId == userId;

            if (!isAdmin && !isCreator)
            {
                return ServiceResponse<DeleteEventDtoResponse>.ForbiddenResponse(
                    "Only the event creator or group admin can delete this event");
            }

            var deletedCount = 0;

            switch (dto.DeleteOption)
            {
                case DeleteRecurringOption.ThisEventOnly:
                    eventToDelete.IsDeleted = true;
                    eventToDelete.DeletedAt = DateTime.UtcNow;
                    deletedCount = 1;
                    break;

                case DeleteRecurringOption.ThisAndFutureEvents:
                    if (eventToDelete.RecurringGroupId.HasValue)
                    {
                        var futureEvents = await context.Events
                            .Where(e => e.RecurringGroupId == eventToDelete.RecurringGroupId
                                        && e.StartTime >= eventToDelete.StartTime
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in futureEvents)
                        {
                            evt.IsDeleted = true;
                            evt.DeletedAt = DateTime.UtcNow;
                        }

                        deletedCount = futureEvents.Count;
                    }
                    else
                    {
                        eventToDelete.IsDeleted = true;
                        eventToDelete.DeletedAt = DateTime.UtcNow;

                        var futureInstances = await context.Events
                            .Where(e => e.RecurringGroupId == eventToDelete.EventId
                                        && e.StartTime >= eventToDelete.StartTime
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in futureInstances)
                        {
                            evt.IsDeleted = true;
                            evt.DeletedAt = DateTime.UtcNow;
                        }

                        deletedCount = 1 + futureInstances.Count;
                    }

                    break;

                case DeleteRecurringOption.AllEvents:
                    if (eventToDelete.RecurringGroupId.HasValue)
                    {
                        var masterEvent = await context.Events
                            .FirstOrDefaultAsync(e => e.EventId == eventToDelete.RecurringGroupId.Value);

                        if (masterEvent != null)
                        {
                            masterEvent.IsDeleted = true;
                            masterEvent.DeletedAt = DateTime.UtcNow;
                            deletedCount++;
                        }

                        var allInstances = await context.Events
                            .Where(e => e.RecurringGroupId == eventToDelete.RecurringGroupId
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in allInstances)
                        {
                            evt.IsDeleted = true;
                            evt.DeletedAt = DateTime.UtcNow;
                        }

                        deletedCount += allInstances.Count;
                    }
                    else
                    {
                        eventToDelete.IsDeleted = true;
                        eventToDelete.DeletedAt = DateTime.UtcNow;

                        var allInstances = await context.Events
                            .Where(e => e.RecurringGroupId == eventToDelete.EventId
                                        && !e.IsDeleted)
                            .ToListAsync();

                        foreach (var evt in allInstances)
                        {
                            evt.IsDeleted = true;
                            evt.DeletedAt = DateTime.UtcNow;
                        }

                        deletedCount = 1 + allInstances.Count;
                    }

                    break;
            }

            await context.SaveChangesAsync();

            var participantIds = await context.EventParticipants
                .Where(ep => ep.EventId == eventToDelete.EventId)
                .Select(ep => ep.UserId)
                .ToListAsync();
            var deleterName = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FirstName)
                .FirstOrDefaultAsync() ?? "Someone";

            await notificationService.NotifyManyAsync(
                participantIds.Where(id => id != userId),
                NotificationType.EventDeleted,
                $"{deleterName} deleted \"{eventToDelete.EventName}\"",
                null,
                eventToDelete.GroupId);

            await activityService.LogActivityAsync(
                ActivityType.EventDeleted,
                userId,
                eventToDelete.GroupId,
                eventToDelete.Group.GroupName,
                eventToDelete.EventId,
                eventToDelete.EventName);

            var response = new DeleteEventDtoResponse
            {
                DeletedCount = deletedCount,
                Message = dto.DeleteOption switch
                {
                    DeleteRecurringOption.ThisEventOnly => "Event deleted successfully",
                    DeleteRecurringOption.ThisAndFutureEvents =>
                        $"{deletedCount} event(s) deleted (this and future events)",
                    DeleteRecurringOption.AllEvents =>
                        $"{deletedCount} event(s) deleted (all recurring instances)",
                    _ => "Event deleted successfully"
                }
            };

            return ServiceResponse<DeleteEventDtoResponse>.SuccessResponse(
                response,
                response.Message);
        }
        catch (Exception e)
        {
            return ServiceResponse<DeleteEventDtoResponse>.ErrorResponse(
                $"Failed to delete event: {e.Message}",
                500);
        }
    }

    public async Task<ServiceResponse<List<GetAllGroupEventsResponse>>> GetAllGroupEventsAsync(int groupId,
        string userId, DateTime rangeStart, DateTime rangeEnd)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<List<GetAllGroupEventsResponse>>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<List<GetAllGroupEventsResponse>>(groupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            var events = await context.Events
                .Where(e => e.GroupId == groupId && !e.IsDeleted && e.StartTime < rangeEnd && e.EndTime > rangeStart)
                .Select(ev => new GetAllGroupEventsResponse
                {
                    EventId = ev.EventId,
                    EventName = ev.EventName,
                    EventDescription = ev.EventDescription,
                    StartTime = ev.StartTime,
                    EndTime = ev.EndTime,
                    CategoryId = ev.CategoryId,
                    CategoryColor = ev.Category!.Color,
                    CategoryName = ev.Category!.Name,
                    Location = ev.Location,
                    CreatedAt = ev.CreatedAt,
                    CreatorUserId = ev.CreatorUserId,
                    CreatorUserEmail = ev.Creator!.Email!,
                    CreatorUserName = ev.Creator!.FirstName,
                    Nickname = ev.Creator.UserMemberSettingsAsTarget
                        .Where(s => s.GroupId == groupId && s.TargetUserId == ev.CreatorUserId).Select(s => s.Nickname)
                        .FirstOrDefault(),
                    IsRecurring = ev.IsRecurring,
                    RecurrenceEndTime = ev.RecurrenceEndTime,
                    RecurrencePattern = ev.RecurrencePattern,
                    UpdatedAt = ev.UpdatedAt,
                    MyRsvpStatus = ev.EventParticipants
                        .Where(ep => ep.UserId == userId)
                        .Select(ep => (RsvpStatus?)ep.Status)
                        .FirstOrDefault()
                })
                .OrderBy(ev => ev.StartTime)
                .ToListAsync();

            return ServiceResponse<List<GetAllGroupEventsResponse>>.SuccessResponse(events);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<GetAllGroupEventsResponse>>.ErrorResponse(
                $"Failed to get all group events, {e}", 500);
        }
    }
    public async Task<ServiceResponse<List<GetAllEventsNextWeekDtoResponse>>> GetAllEventsNextWeekAsync(string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<List<GetAllEventsNextWeekDtoResponse>>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var now = DateTime.UtcNow;
            var nextWeek = now.AddDays(7);

            var events = await context.Events
                .Where(e => !e.IsDeleted
                            && e.StartTime >= now
                            && e.StartTime <= nextWeek
                            && e.Group.GroupUsers.Any(gu => gu.UserId == userId && gu.IsActive))
                .Select(e => new GetAllEventsNextWeekDtoResponse
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    CategoryId = e.CategoryId,
                    CategoryColor = e.Category!.Color,
                    CategoryName = e.Category!.Name,
                    Location = e.Location,
                    CreatorUserId = e.CreatorUserId,
                    CreatorUserName = e.Creator.FirstName,
                    CreatorUserEmail = e.Creator.Email!,
                    Nickname = e.Creator.UserMemberSettingsAsTarget
                        .Where(s => s.GroupId == e.GroupId && s.TargetUserId == e.CreatorUserId)
                        .Select(s => s.Nickname)
                        .FirstOrDefault(),
                    UpdatedAt = e.UpdatedAt,
                    GroupName = e.Group.GroupName,
                    MyRsvpStatus = e.EventParticipants
                        .Where(ep => ep.UserId == userId)
                        .Select(ep => (RsvpStatus?)ep.Status)
                        .FirstOrDefault()
                })
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            return ServiceResponse<List<GetAllEventsNextWeekDtoResponse>>.SuccessResponse(events);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<GetAllEventsNextWeekDtoResponse>>.ErrorResponse(
                $"Failed to get events for next week: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<NextEventDtoResponse?>> GetNextEvent(int groupId, string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
            await userManager.ValidateUserAsync<NextEventDtoResponse>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<NextEventDtoResponse>(groupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            var nextEvent = await context.Events
            .Where(ne => ne.GroupId == groupId && !ne.IsDeleted && ne.StartTime > DateTime.UtcNow)
            .OrderBy(ne => ne.StartTime)
            .Select(ev => new NextEventDtoResponse
            {
                EventName = ev.EventName,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                CategoryId = ev.CategoryId,
                CategoryColor = ev.Category!.Color
            })
            .FirstOrDefaultAsync();
            if (nextEvent == null)
            {
                return ServiceResponse<NextEventDtoResponse?>.SuccessResponse(null);
            }

            return ServiceResponse<NextEventDtoResponse?>.SuccessResponse(nextEvent);

        }
        catch (Exception e)
        {
            return ServiceResponse<NextEventDtoResponse?>.ErrorResponse(
                $"Failed to get next event, {e}", 500);
        }
    }



    public async Task<ServiceResponse<FindFreeSlotDtoResponse?>> FindFreeSlotAsync(
        int groupId,
        string userId,
        int durationMinutes,
        int lookAheadDays)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<FindFreeSlotDtoResponse>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<FindFreeSlotDtoResponse>(groupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            if (durationMinutes <= 0)
            {
                return ServiceResponse<FindFreeSlotDtoResponse?>.ErrorResponse(
                    "Duration must be greater than 0 minutes");
            }

            if (lookAheadDays <= 0 || lookAheadDays > 365)
            {
                return ServiceResponse<FindFreeSlotDtoResponse?>.ErrorResponse(
                    "Look-ahead days must be between 1 and 365");
            }

            var rangeStart = DateTime.UtcNow;
            var rangeEnd = rangeStart.AddDays(lookAheadDays);
            var duration = TimeSpan.FromMinutes(durationMinutes);

            if (rangeEnd - rangeStart < duration)
            {
                return ServiceResponse<FindFreeSlotDtoResponse?>.ErrorResponse(
                    "Look-ahead window is shorter than the requested duration");
            }

            var memberIds = await context.GroupUsers
                .Where(gu => gu.GroupId == groupId && gu.IsActive)
                .Select(gu => gu.UserId)
                .ToListAsync();

            if (memberIds.Count == 0)
            {
                return ServiceResponse<FindFreeSlotDtoResponse?>.ErrorResponse("Group has no active members");
            }

            // Every event in any group a member belongs to blocks that member's time,
            // not just events in the target group — unless that member has declined it.
            // A missing participant row (events created before RSVP existed) is treated as busy.
            var busyIntervals = await context.Events
                .Where(e => !e.IsDeleted
                            && e.StartTime < rangeEnd
                            && e.EndTime > rangeStart
                            && e.Group.GroupUsers.Any(gu =>
                                memberIds.Contains(gu.UserId)
                                && gu.IsActive
                                && !e.EventParticipants.Any(ep =>
                                    ep.UserId == gu.UserId && ep.Status == RsvpStatus.Declined)))
                .OrderBy(e => e.StartTime)
                .Select(e => new { e.StartTime, e.EndTime })
                .ToListAsync();

            var mergedBusyBlocks = new List<(DateTime Start, DateTime End)>();
            foreach (var busy in busyIntervals)
            {
                var start = busy.StartTime < rangeStart ? rangeStart : busy.StartTime;
                var end = busy.EndTime;
                if (end <= start)
                {
                    continue;
                }

                if (mergedBusyBlocks.Count > 0 && start <= mergedBusyBlocks[^1].End)
                {
                    if (end > mergedBusyBlocks[^1].End)
                    {
                        mergedBusyBlocks[^1] = (mergedBusyBlocks[^1].Start, end);
                    }
                }
                else
                {
                    mergedBusyBlocks.Add((start, end));
                }
            }

            var roundingInterval = TimeSpan.FromMinutes(5);
            var candidateStart = RoundUpTo(rangeStart, roundingInterval);
            foreach (var block in mergedBusyBlocks)
            {
                if (candidateStart + duration <= block.Start)
                {
                    break;
                }

                if (block.End > candidateStart)
                {
                    candidateStart = RoundUpTo(block.End, roundingInterval);
                }
            }

            if (candidateStart + duration > rangeEnd)
            {
                return ServiceResponse<FindFreeSlotDtoResponse?>.SuccessResponse(
                    null,
                    $"No free slot of {durationMinutes} minutes found within the next {lookAheadDays} day(s)");
            }

            var freeSlot = new FindFreeSlotDtoResponse
            {
                StartTime = candidateStart,
                EndTime = candidateStart + duration,
                DurationMinutes = durationMinutes
            };

            return ServiceResponse<FindFreeSlotDtoResponse?>.SuccessResponse(freeSlot);
        }
        catch (Exception e)
        {
            return ServiceResponse<FindFreeSlotDtoResponse?>.ErrorResponse(
                $"Failed to find free slot: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<RespondToEventDtoResponse>> RespondToEventAsync(
        RespondToEventDtoRequest dto,
        string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<RespondToEventDtoResponse>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            if (!Enum.IsDefined(typeof(RsvpStatus), dto.Status))
            {
                return ServiceResponse<RespondToEventDtoResponse>.ErrorResponse(
                    "Invalid RSVP status. Valid values are: Pending, Accepted, Declined");
            }

            var participant = await context.EventParticipants
                .Include(ep => ep.Event)
                .FirstOrDefaultAsync(ep => ep.EventId == dto.EventId && ep.UserId == userId);

            if (participant == null || participant.Event.IsDeleted)
            {
                return ServiceResponse<RespondToEventDtoResponse>.NotFoundResponse(
                    "You have not been invited to this event");
            }

            participant.Status = dto.Status;
            participant.RespondedAt = dto.Status == RsvpStatus.Pending ? null : DateTime.UtcNow;

            await context.SaveChangesAsync();

            if (dto.Status != RsvpStatus.Pending && participant.Event.CreatorUserId != userId)
            {
                var responseVerb = dto.Status switch
                {
                    RsvpStatus.Accepted => "accepted",
                    RsvpStatus.Declined => "declined",
                    RsvpStatus.Maybe => "responded maybe to",
                    _ => "responded to"
                };

                await notificationService.NotifyAsync(
                    participant.Event.CreatorUserId,
                    NotificationType.RsvpResponse,
                    $"{user!.FirstName} {responseVerb} your event \"{participant.Event.EventName}\"",
                    participant.EventId,
                    participant.Event.GroupId);
            }

            if (dto.Status != RsvpStatus.Pending)
            {
                var rsvpGroupName = await context.Groups
                    .Where(g => g.GroupId == participant.Event.GroupId)
                    .Select(g => g.GroupName)
                    .FirstOrDefaultAsync() ?? "your group";

                await activityService.LogActivityAsync(
                    ActivityType.RsvpResponse,
                    userId,
                    participant.Event.GroupId,
                    rsvpGroupName,
                    participant.EventId,
                    participant.Event.EventName);
            }

            var response = new RespondToEventDtoResponse
            {
                EventId = participant.EventId,
                Status = participant.Status,
                RespondedAt = participant.RespondedAt
            };

            return ServiceResponse<RespondToEventDtoResponse>.SuccessResponse(
                response,
                $"RSVP updated to {dto.Status}");
        }
        catch (Exception e)
        {
            return ServiceResponse<RespondToEventDtoResponse>.ErrorResponse(
                $"Failed to respond to event: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<List<EventParticipantDtoResponse>>> GetEventParticipantsAsync(
        int eventId,
        string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<List<EventParticipantDtoResponse>>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var eventEntity = await context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);

            if (eventEntity == null)
            {
                return ServiceResponse<List<EventParticipantDtoResponse>>.NotFoundResponse("Event not found");
            }

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<List<EventParticipantDtoResponse>>(
                    eventEntity.GroupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            var groupId = eventEntity.GroupId;
            var participants = await context.EventParticipants
                .Where(ep => ep.EventId == eventId)
                .Select(ep => new EventParticipantDtoResponse
                {
                    UserId = ep.UserId,
                    Email = ep.User.Email!,
                    FirstName = ep.User.FirstName,
                    LastName = ep.User.LastName,
                    Nickname = ep.User.UserMemberSettingsAsTarget
                        .Where(s => s.GroupId == groupId && s.TargetUserId == ep.UserId)
                        .Select(s => s.Nickname)
                        .FirstOrDefault(),
                    Status = ep.Status,
                    InvitedAt = ep.InvitedAt,
                    RespondedAt = ep.RespondedAt
                })
                .OrderBy(ep => ep.FirstName)
                .ToListAsync();

            return ServiceResponse<List<EventParticipantDtoResponse>>.SuccessResponse(participants);
        }
        catch (Exception e)
        {
            return ServiceResponse<List<EventParticipantDtoResponse>>.ErrorResponse(
                $"Failed to get event participants: {e.Message}", 500);
        }
    }

    public async Task<ServiceResponse<GetEventDtoResponse>> GetEventAsync(
        int groupId,
        int eventId,
        string userId)
    {
        try
        {
            var (isValidUser, errorResponseUser, user) =
                await userManager.ValidateUserAsync<GetEventDtoResponse>(userId);
            if (!isValidUser)
                return errorResponseUser!;

            var (isValidMember, errorMember, member) =
                await context.ValidateGroupMemberAsync<GetEventDtoResponse>(groupId, userId);
            if (!isValidMember || member == null)
            {
                return errorMember!;
            }

            var eventDto = await context.Events
                .Where(ev => ev.EventId == eventId && ev.GroupId == groupId && !ev.IsDeleted)
                .Select(ev => new GetEventDtoResponse
                {
                    EventId = ev.EventId,
                    EventName = ev.EventName,
                    EventDescription = ev.EventDescription,
                    GroupId = ev.GroupId,
                    StartTime = ev.StartTime,
                    EndTime = ev.EndTime,
                    CategoryId = ev.CategoryId,
                    CategoryColor = ev.Category!.Color,
                    CategoryName = ev.Category!.Name,
                    Location = ev.Location,
                    CreatorUserId = ev.CreatorUserId,
                    CreatorUserEmail = ev.Creator!.Email!,
                    CreatorUserName = ev.Creator!.FirstName,
                    Nickname = ev.Creator.UserMemberSettingsAsTarget
                        .Where(s => s.GroupId == groupId && s.TargetUserId == ev.CreatorUserId)
                        .Select(s => s.Nickname)
                        .FirstOrDefault(),
                    IsRecurring = ev.IsRecurring,
                    RecurrencePattern = ev.RecurrencePattern,
                    RecurrenceEndTime = ev.RecurrenceEndTime,
                    CreatedAt = ev.CreatedAt,
                    UpdatedAt = ev.UpdatedAt,
                    MyRsvpStatus = ev.EventParticipants
                        .Where(ep => ep.UserId == userId)
                        .Select(ep => (RsvpStatus?)ep.Status)
                        .FirstOrDefault(),
                    Participants = ev.EventParticipants.Select(ep => new EventParticipantDtoResponse
                    {
                        UserId = ep.UserId,
                        Email = ep.User.Email!,
                        FirstName = ep.User.FirstName,
                        LastName = ep.User.LastName,
                        Nickname = ep.User.UserMemberSettingsAsTarget
                            .Where(s => s.GroupId == groupId && s.TargetUserId == ep.UserId)
                            .Select(s => s.Nickname)
                            .FirstOrDefault(),
                        Status = ep.Status,
                        InvitedAt = ep.InvitedAt,
                        RespondedAt = ep.RespondedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (eventDto == null)
            {
                return ServiceResponse<GetEventDtoResponse>.NotFoundResponse("Event not found");
            }

            return ServiceResponse<GetEventDtoResponse>.SuccessResponse(eventDto);
        }
        catch (Exception e)
        {
            return ServiceResponse<GetEventDtoResponse>.ErrorResponse(
                $"Failed to get event: {e.Message}", 500);
        }
    }

    private static List<EventParticipant> BuildEventParticipants(int eventId, List<string> memberIds,
        string creatorUserId)
    {
        var now = DateTime.UtcNow;
        return memberIds.Select(id => id == creatorUserId
            ? new EventParticipant
            {
                EventId = eventId,
                UserId = id,
                InvitedAt = now,
                Status = RsvpStatus.Accepted,
                RespondedAt = now
            }
            : new EventParticipant
            {
                EventId = eventId,
                UserId = id,
                InvitedAt = now,
                Status = RsvpStatus.Pending,
                RespondedAt = null
            }).ToList();
    }

    private static DateTime RoundUpTo(DateTime time, TimeSpan interval)
    {
        var overflow = time.Ticks % interval.Ticks;
        return overflow == 0 ? time : time.AddTicks(interval.Ticks - overflow);
    }

    private void UpdateSingleEvent(Event eventToUpdate, UpdateEventDtoRequest dto)
    {
        eventToUpdate.EventName = dto.EventName;
        eventToUpdate.EventDescription = dto.EventDescription;
        eventToUpdate.Location = dto.Location;
        eventToUpdate.StartTime = dto.StartTime;
        eventToUpdate.EndTime = dto.EndTime;
        eventToUpdate.UpdatedAt = DateTime.UtcNow;
    }


    private void UpdateEventWithTimeOfDay(Event eventToUpdate, UpdateEventDtoRequest dto,
        TimeSpan newStartTime, TimeSpan newEndTime)
    {
        eventToUpdate.EventName = dto.EventName;
        eventToUpdate.EventDescription = dto.EventDescription;
        eventToUpdate.Location = dto.Location;

        eventToUpdate.StartTime = eventToUpdate.StartTime.Date + newStartTime;
        eventToUpdate.EndTime = eventToUpdate.EndTime.Date + newEndTime;

        eventToUpdate.UpdatedAt = DateTime.UtcNow;
    }


    private async Task<int> CreateRecurringEventsAsync(RecurrencePattern? recurrencePattern, Event baseEvent,
        List<string> memberIds, string creatorUserId)
    {
        try
        {
            var endDate = baseEvent.RecurrenceEndTime ??
                          GetDefaultRecurenceEndDate(baseEvent.StartTime, recurrencePattern);
            var eventsToAdd = new List<Event>();
            var currentStartTime = baseEvent.StartTime;
            var eventDuration = baseEvent.EndTime - baseEvent.StartTime;

            const int maxOccurrences = 1000;
            int createdCount = 0;
            var recurringGroupId = baseEvent.EventId;
            while (currentStartTime < endDate && createdCount < maxOccurrences)
            {
                currentStartTime = GetNextOccurrence(currentStartTime, recurrencePattern);
                if (currentStartTime > endDate)
                    break;
                var currentEndTime = currentStartTime.Add(eventDuration);
                var recurringEvent = new Event
                {
                    EventName = baseEvent.EventName,
                    EventDescription = baseEvent.EventDescription,
                    GroupId = baseEvent.GroupId,
                    StartTime = currentStartTime,
                    EndTime = currentEndTime,
                    CategoryId = baseEvent.CategoryId,
                    Location = baseEvent.Location,
                    CreatorUserId = baseEvent.CreatorUserId,
                    IsRecurring = true,
                    RecurrencePattern = recurrencePattern,
                    RecurrenceEndTime = baseEvent.RecurrenceEndTime,
                    RecurringGroupId = recurringGroupId,
                    CreatedAt = DateTime.UtcNow
                };
                eventsToAdd.Add(recurringEvent);
                createdCount++;
            }

            if (eventsToAdd.Any())
            {
                await context.Events.AddRangeAsync(eventsToAdd);
                await context.SaveChangesAsync();

                var participants = eventsToAdd
                    .SelectMany(evt => BuildEventParticipants(evt.EventId, memberIds, creatorUserId));
                context.EventParticipants.AddRange(participants);
                await context.SaveChangesAsync();
            }

            return createdCount;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create recurring events: {e.Message}");
            return 0;
        }
    }

    private DateTime GetNextOccurrence(DateTime current, RecurrencePattern? pattern)
    {
        return pattern switch
        {
            RecurrencePattern.Daily => current.AddDays(1),
            RecurrencePattern.Weekly => current.AddDays(7),
            RecurrencePattern.Monthly => current.AddMonths(1),
            RecurrencePattern.Yearly => current.AddYears(1),
            _ => current
        };
    }

    private static DateTime GetDefaultRecurenceEndDate(DateTime startDate, RecurrencePattern? pattern)
    {
        return pattern switch
        {
            RecurrencePattern.Daily => startDate.AddMonths(3),
            RecurrencePattern.Weekly => startDate.AddYears(1),
            RecurrencePattern.Monthly => startDate.AddYears(2),
            RecurrencePattern.Yearly => startDate.AddYears(5),
            _ => startDate.AddYears(1)
        };
    }
}