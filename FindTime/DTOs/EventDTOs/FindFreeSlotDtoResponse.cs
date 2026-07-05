namespace FindTime.DTOs.EventDTOs
{
    public class FindFreeSlotDtoResponse
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
    }
}
