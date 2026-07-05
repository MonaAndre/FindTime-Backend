using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public enum ActivityType
{
    EventCreated = 0,
    EventUpdated = 1,
    EventDeleted = 2,
    MemberJoined = 3,
    RsvpResponse = 4
}

public class Activity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ActivityId { get; set; }

    [Required]
    public ActivityType Type { get; set; }

    [Required]
    public string ActorUserId { get; set; } = string.Empty;

    [Required]
    public int GroupId { get; set; }

    // Snapshot, not a live join - must survive the group being renamed later.
    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    public int? EventId { get; set; }

    // Snapshot, not a live join - must survive event deletion/rename.
    [MaxLength(200)]
    public string? EventName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ActorUserId))]
    public virtual ApplicationUser ActorUser { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public virtual Event? Event { get; set; }
}
