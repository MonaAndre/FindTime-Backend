using FindTime.Models;

namespace FindTime.DTOs.EventDTOs;

public class EventParticipantDtoResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public RsvpStatus Status { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
