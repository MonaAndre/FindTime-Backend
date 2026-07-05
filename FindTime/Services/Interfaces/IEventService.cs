using FindTime.Common;
using FindTime.DTOs.EventDTOs;

namespace FindTime.Services.Interfaces;

public interface IEventService
{
    Task<ServiceResponse<CreateEventDtoResponse>> CreateEventAsync(CreateEventDtoRequest dto, string userId);

    Task<ServiceResponse<UpdateEventDtoResponse>> UpdateEventAsync(
        UpdateEventDtoRequest dto,
        string userId);

    Task<ServiceResponse<DeleteEventDtoResponse>> DeleteEventAsync(
        DeleteEventDtoRequest dto,
        string userId);

    Task<ServiceResponse<List<GetAllGroupEventsResponse>>> GetAllGroupEventsAsync(int groupId,
        string userId, DateTime rangeStart, DateTime randgeEnd);
    Task<ServiceResponse<NextEventDtoResponse?>> GetNextEvent(int groupId, string userId);
    Task<ServiceResponse<List<GetAllEventsNextWeekDtoResponse>>> GetAllEventsNextWeekAsync(string userId);

    Task<ServiceResponse<FindFreeSlotDtoResponse?>> FindFreeSlotAsync(
        int groupId,
        string userId,
        int durationMinutes,
        int lookAheadDays);

    Task<ServiceResponse<RespondToEventDtoResponse>> RespondToEventAsync(
        RespondToEventDtoRequest dto,
        string userId);

    Task<ServiceResponse<List<EventParticipantDtoResponse>>> GetEventParticipantsAsync(
        int eventId,
        string userId);

    Task<ServiceResponse<GetEventDtoResponse>> GetEventAsync(
        int groupId,
        int eventId,
        string userId);
}