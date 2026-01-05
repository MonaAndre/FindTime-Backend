using FindTime.Common;
using FindTime.DTOs.EventDTOs;

namespace FindTime.Services.Interfaces;

public interface IEventService
{
 Task<ServiceResponse<CreateEventDtoResponse>> CreateEventAsync(CreateEventDtoRequest dto, string userId);
}