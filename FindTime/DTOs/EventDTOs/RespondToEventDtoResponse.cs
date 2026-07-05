using FindTime.Models;

namespace FindTime.DTOs.EventDTOs;

public class RespondToEventDtoResponse
{
    public int EventId { get; set; }
    public RsvpStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
}
