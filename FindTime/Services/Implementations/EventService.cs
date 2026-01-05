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
                CreatedAt = DateTime.UtcNow,
            };

            await context.Events.AddAsync(newEvent);
            await context.SaveChangesAsync();

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
                RecurrencePattern = newEvent.RecurrencePattern
            };
            return ServiceResponse<CreateEventDtoResponse>.SuccessResponse(createdEvent, "Event created");
        }
        catch (Exception e)
        {
            return ServiceResponse<CreateEventDtoResponse>.ErrorResponse($"Failed to create event: {e.Message}");
        }
    }
    //CreatEventAsync
}