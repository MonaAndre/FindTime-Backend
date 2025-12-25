using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GroupId { get; set; }

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    [Required]
    public string AdminId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AdminId))]
    public virtual ApplicationUser Admin { get; set; } = null!;
    
    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<UserGroupSettings> UserGroupSettings { get; set; } = new List<UserGroupSettings>();
    public virtual ICollection<UserMemberSettings> UserMemberSettings { get; set; } = new List<UserMemberSettings>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}