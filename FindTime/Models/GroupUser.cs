using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class GroupUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GroupUserId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int GroupId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;
}