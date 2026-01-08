namespace FindTime.DTOs.EventDTOs;

public class DeleteEventDtoRequest
{
    public int EventId { get; set; }

    public DeleteRecurringOption DeleteOption { get; set; } = DeleteRecurringOption.ThisEventOnly;
}

public enum DeleteRecurringOption
{
    ThisEventOnly,
    ThisAndFutureEvents,
    AllEvents
}