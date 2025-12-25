using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

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

    // Navigation properties
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}
