using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class UserGroupSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserGroupSettingsId { get; set; }

    [Required] public string UserId { get; set; } = string.Empty;

    [Required] public int GroupId { get; set; }

    [Required]
    [MaxLength(7)] // Hex color code
    public string GroupColor { get; set; } = "zinc";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(GroupId))] public virtual Group Group { get; set; } = null!;
}