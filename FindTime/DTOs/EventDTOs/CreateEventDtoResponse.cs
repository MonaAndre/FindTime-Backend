using FindTime.Models;

namespace FindTime.DTOs.EventDTOs;

public class CreateEventDtoResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public int GroupId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public bool IsRecurring { get; set; } = false;
    public RecurrencePattern? RecurrencePattern { get; set; }

}