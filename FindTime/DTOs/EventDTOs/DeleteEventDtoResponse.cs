namespace FindTime.DTOs.EventDTOs;

public class DeleteEventDtoResponse
{
    public int DeletedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}