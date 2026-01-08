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
}