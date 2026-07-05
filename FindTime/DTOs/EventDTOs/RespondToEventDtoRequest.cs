using FindTime.Models;

namespace FindTime.DTOs.EventDTOs;

public class RespondToEventDtoRequest
{
    public int EventId { get; set; }
    public RsvpStatus Status { get; set; }
}
