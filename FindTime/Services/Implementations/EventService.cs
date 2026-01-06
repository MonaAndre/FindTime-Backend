using FindTime.Common;
using FindTime.Data;
using FindTime.DTOs.EventDTOs;
using FindTime.Extensions;
using FindTime.Models;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

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
                    : "Event created");        }
        catch (Exception e)
        {
            return ServiceResponse<CreateEventDtoResponse>.ErrorResponse($"Failed to create event: {e.Message}");
        }
    }

    private async Task<int> CreateRecurringEventsAsync(RecurrencePattern? recurrencePattern, Event baseEvent)
    {
        try
        {
            var endDate = baseEvent.RecurrenceEndTime ?? GetDefaultRecurenceEndDate(baseEvent.StartTime, recurrencePattern);
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

    private DateTime GetDefaultRecurenceEndDate(DateTime startDate, RecurrencePattern? pattern)
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