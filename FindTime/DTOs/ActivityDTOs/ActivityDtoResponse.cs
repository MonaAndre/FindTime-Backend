using FindTime.Models;

namespace FindTime.DTOs.ActivityDTOs;

public class ActivityDtoResponse
{
    public int ActivityId { get; set; }
    public ActivityType Type { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorFirstName { get; set; } = string.Empty;
    public string ActorLastName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public DateTime CreatedAt { get; set; }
}
