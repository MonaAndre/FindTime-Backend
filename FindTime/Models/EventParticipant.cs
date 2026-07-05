using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public enum RsvpStatus
{
    Pending,
    Accepted,
    Declined,
    Maybe
}

public class EventParticipant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventParticipantId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public RsvpStatus Status { get; set; } = RsvpStatus.Pending;

    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}
