using Microsoft.AspNetCore.Identity;

namespace FindTime.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public DateTime RegisterDay { get; set; } = DateTime.UtcNow;
    public bool IsConfirmed { get; set; } = false;
    public string? RefreshToken { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? ProfilePicLink { get; set; }

    // Navigation properties
    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
    public virtual ICollection<EventNote> EventNotes { get; set; } = new List<EventNote>();
    public virtual ICollection<EventReminder> EventReminders { get; set; } = new List<EventReminder>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<UserGroupSettings> UserGroupSettings { get; set; } = new List<UserGroupSettings>();
    public virtual ICollection<UserMemberSettings> UserMemberSettingsAsUser { get; set; } = new List<UserMemberSettings>();
    public virtual ICollection<UserMemberSettings> UserMemberSettingsAsTarget { get; set; } = new List<UserMemberSettings>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
