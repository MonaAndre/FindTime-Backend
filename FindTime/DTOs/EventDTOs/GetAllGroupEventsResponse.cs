using FindTime.Models;

namespace FindTime.DTOs.EventDTOs;

public class GetAllGroupEventsResponse
{
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string? EventDescription { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryColor { get; set; }
    public string? Location { get; set; }
    public string CreatorUserId { get; set; } = string.Empty;
    public string CreatorUserName { get; set; } = string.Empty;
    public string CreatorUserEmail { get; set; } = string.Empty;
    public string? Nickname { get; set; } 
    public bool IsRecurring { get; set; } = false;
    public RecurrencePattern? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}