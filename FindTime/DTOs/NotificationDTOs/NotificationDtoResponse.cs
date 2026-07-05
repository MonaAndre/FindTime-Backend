using FindTime.Models;

namespace FindTime.DTOs.NotificationDTOs;

public class NotificationDtoResponse
{
    public int NotificationId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public int? GroupId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
