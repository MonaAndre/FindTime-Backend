using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class UserMemberSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserMemberSettingsId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;

  
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    [Required]
    public int GroupId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nickname { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(TargetUserId))]
    public virtual ApplicationUser TargetUser { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;
}