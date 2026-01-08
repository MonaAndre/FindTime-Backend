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
}