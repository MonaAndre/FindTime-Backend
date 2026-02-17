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

public class EventService(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : IEventService
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
            int recurringInstancesCreated = 0;
            if (dto.IsRecurring && dto.RecurrencePattern.HasValue)
            {
                recurringInstancesCreated = await CreateRecurringEventsAsync(
                    dto.RecurrencePattern.Value,
                    newEvent);

                if (recurringInstancesCreated == 0)
                {
                    Console.WriteLine(
                        $"Warning: Failed to create recurring instances for event {newEvent.EventId}");
                }
            }


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
        string userId)
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
                .Where(e => e.GroupId == groupId && !e.IsDeleted)
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
                    UpdatedAt = ev.UpdatedAt
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


    private async Task<int> CreateRecurringEventsAsync(RecurrencePattern? recurrencePattern, Event baseEvent)
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