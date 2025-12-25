using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public enum RecurrencePattern
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class Event
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventId { get; set; }

    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? EventDescription { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public int? CategoryId { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Required]
    public string CreatorUserId { get; set; } = string.Empty;

    public bool IsRecurring { get; set; } = false;
    
    public RecurrencePattern? RecurrencePattern { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    [ForeignKey(nameof(CreatorUserId))]
    public virtual ApplicationUser Creator { get; set; } = null!;

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
    public virtual ICollection<EventNote> EventNotes { get; set; } = new List<EventNote>();
    public virtual ICollection<EventReminder> EventReminders { get; set; } = new List<EventReminder>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
