using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class EventReminder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventReminderId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Number of minutes before the event to send reminder
    /// Example: 15 = 15 minutes before, 60 = 1 hour before, 1440 = 1 day before
    /// </summary>
    [Required]
    public int MinutesBeforeEvent { get; set; }

    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}
