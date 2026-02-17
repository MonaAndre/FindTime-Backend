namespace FindTime.DTOs.EventDTOs
{
    public class NextEventDtoResponse
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryColor { get; set; }

    }
}
